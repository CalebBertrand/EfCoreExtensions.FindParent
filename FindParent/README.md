# FindParent
```
public static IQueryable<TParent> FindParent<TChild, TParent>(this DbContext context, object childId)
```

An entity framework core extension method to find a parent entity any number of ancestors away.
Traverses the db context model to find the optimal path, then dynamically generates a queryable of chained selects along the path.
Example:

Say there is a table structure where Garages have multiple Cars, Cars have multiple Tires, and Tires have multiple Bolts.
If you wanted to know the parent Garage of the Bolt with an Id of 1, you could get it via:

````
  Garage garageFromBolt = context.FindParent<Bolt, Garage>(1).FirstOrDefault();
````

The method uses Dijkstra’s shortest path algorithm to find the shortest path to the parent.
(Meaning the translated SQL has the lowest number of expensive joins!)

## Possible Exceptions
1. If the child table has a composite key, will throw a NotSupportedException.
2. If either the child or parent Types do not correspond to tables in the context model, will throw an ArgumentException.
3. If the child id's type is not assignable to the child table's primary key property, will throw an ArgumentException.
