// <copyright file="FindParent.cs">
// Written by Caleb Bertrand, under the MIT licence.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// Db Context extensions for finding related entities without explicitly defining the joins required.
/// </summary>
public static class EfCoreExtensions
{
    /// <summary>
    /// Finds the parent of type <typeparamref name="TParent"/> for the starting
    /// entity of type <typeparamref name="TChild"/>.
    /// </summary>
    /// <param name="context">The db context.</param>
    /// <param name="childId">The child identifier.</param>
    /// <typeparam name="TChild">The child type (a user defined ef model).</typeparam>
    /// <typeparam name="TParent">The parent type (a user defined ef model).</typeparam>
    /// <returns>The parent entity.</returns>
    public static IQueryable<TParent> FindParent<TChild, TParent>(this DbContext context, object childId)
        where TParent : class
        where TChild : class
    {
        IProperty sourceTableIdProp;
        try
        {
            sourceTableIdProp = context.Model.FindEntityType(typeof(TChild))?.FindPrimaryKey().Properties.Single();
        }
        catch
        {
            throw new NotSupportedException("The child table must not have a composite primary key.");
        }

        if (sourceTableIdProp == null)
            throw new ArgumentException("The child type is not associated with any table.");

        if (!childId.GetType().IsAssignableTo(sourceTableIdProp.PropertyInfo.PropertyType))
            throw new ArgumentException("The child id is not assignable to the id property of the specified TChild.");
        
        var sourceQueryable = FilterByChildId(context.Set<TChild>(), sourceTableIdProp.Name, childId);

        if (typeof(TChild) == typeof(TParent)) return (IQueryable<TParent>)sourceQueryable;

        if (context.Model.FindEntityType(typeof(TParent)) == null)
            throw new ArgumentException("The parent type is not associated with any table.");

        var firstStep = GetRoute<TChild, TParent>(context);
        return GetParentQueryableFromStep<TParent>(firstStep, sourceQueryable);
    }

    /// <summary>
    /// Gets a queryable with a where clause attached, filtering out entities without the childId.
    /// </summary>
    /// <param name="dbSet">The dbSet for the entity in question.</param>
    /// <param name="idPropName">The name of the field or property which serves as the primary key.</param>
    /// <param name="childId">The entity identifier.</param>
    /// <typeparam name="TChild">The child entity type.</typeparam>
    private static IQueryable<TChild> FilterByChildId<TChild>(DbSet<TChild> dbSet, string idPropName, object childId)
        where TChild : class
    {
        // Build a dynamic where lambda to filter out entities without the specified id
        var param = Expression.Parameter(typeof(TChild));
        var body = Expression.Equal(Expression.PropertyOrField(param, idPropName), Expression.Constant(childId));

        var queryableDbSet = dbSet.AsQueryable();
        return (IQueryable<TChild>)queryableDbSet.Provider.CreateQuery(
            Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Where),
                new[] { typeof(TChild) },
                queryableDbSet.Expression,
                Expression.Lambda(body, param)
            )
        );
    }

    /// <summary>
    /// Gets a queryable which will evaluate to the parent entity. Uses a recursive strategy.
    /// </summary>
    /// <param name="currentStep">A step along the path to the parent entity.</param>
    /// <param name="query">The query so built far.</param>
    /// <typeparam name="TParent">The type of the parent entity.</typeparam>
    private static IQueryable<TParent> GetParentQueryableFromStep<TParent>(Step currentStep, IQueryable query)
        where TParent : class
    {
        if (currentStep.NextStep == null)
            return (IQueryable<TParent>)query;

        // Build a dynamic select lambda to select the next table entity
        var param = Expression.Parameter(currentStep.EntityType.ClrType);
        var body = Expression.PropertyOrField(param, currentStep.NextStepPropName!);

        IQueryable queryNextTable = query.Provider.CreateQuery(
            Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Select),
                new[] { currentStep.EntityType.ClrType, currentStep.NextStep.EntityType.ClrType },
                query.Expression,
                Expression.Lambda(body, param)
            )
        );
        return GetParentQueryableFromStep<TParent>(currentStep.NextStep, queryNextTable);
    }

    /// <summary>
    /// Returns a chain of steps which can be taken to reach the parent table from the child table.
    /// Treats tables as nodes in a graph and finds the steps via Dijkstra's algorithm.
    /// </summary>
    /// <param name="context">The db context.</param>
    private static Step GetRoute<TChild, TParent>(DbContext context)
    {
        var startingTable = context.Model.FindEntityType(typeof(TChild));
        var targetTable = context.Model.FindEntityType(typeof(TParent));

        // A collection of the tables traversed and the cost of reaching each.
        var costs = new Dictionary<IEntityType, (int Cost, bool IsEdge, IEntityType? From)> { { startingTable!, (0, false, null) } };
        var currentTable = costs.Keys.First();

        do
        {
            var (currCost, _, currFrom) = costs[currentTable];

            var connections = currentTable.GetForeignKeys()
                .Select(fk => fk.PrincipalEntityType)
                .ToList();

            foreach (var connection in connections)
            {
                if (costs.TryGetValue(connection, out var adjacentTable))
                {
                    costs[connection] = (Math.Min(adjacentTable.Cost, currCost + 1), adjacentTable.IsEdge,
                        currCost + 1 < adjacentTable.Cost ? currentTable : adjacentTable.From);
                }
                else
                {
                    costs.Add(connection, (currCost + 1, true, currentTable));
                }
            }

            if (connections.Contains(targetTable))
                break;
            
            costs[currentTable] = (currCost, false, currFrom); 

            // Get the edge table (ie table with potentially unexplored connections) that has the lowest cost
            currentTable = costs
                .Where(table => table.Value.IsEdge)
                .Aggregate((smallest, current) =>
                    current.Value.Cost < smallest.Value.Cost ? current : smallest).Key;
        } while (costs.Any(table => table.Value.IsEdge));

        var currentStep = new Step(targetTable); // Will be reassigned repeatedly as we work back to source
        while (costs[currentStep.EntityType].From != null)
        {
            var previousStep = new Step(costs[currentStep.EntityType].From!, currentStep);
            currentStep = previousStep;
        }

        return currentStep;
    }
    
    /// <summary>
    /// Represents a step in the path to a target table.
    /// </summary>
    internal class Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Step"/> class.
        /// </summary>
        /// <param name="entityType">The entity type associated with the table at this step.</param>
        /// <param name="nextStep">The next step.</param>
        public Step(IEntityType entityType, Step? nextStep = null)
        {
            this.EntityType = entityType;
            this.NextStep = nextStep;
            if (this.NextStep != null)
            {
                this.NextStepPropName = entityType.GetNavigations()
                    .First(nav => nav.TargetEntityType == this.NextStep.EntityType).Name;
            }
        }

        /// <summary>
        /// Gets the entity type associated with the table at this step.
        /// </summary>
        public IEntityType EntityType { get; }

        /// <summary>
        /// Gets the next step.
        /// </summary>
        public Step? NextStep { get; }

        /// <summary>
        /// Gets the name of the navigation property to the next table.
        /// </summary>
        public string? NextStepPropName { get; }
    }
}