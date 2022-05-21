# FindParent
An entity framework core extension method to find a parent entity any number of ancestors away.
Traverses the db context model to find the optimal path, then dynamically generates a queryable of chained selects along the path.
Example:

Say there is a table structure where Garages have multiple Cars, Cars have multiple Tires, and Tires have multiple Bolts.
If you wanted to know the parent Garage of the Bolt with an Id of 1, you could get it via:

````
  Garage garageFromBolt = context.FindParent&lsaquo;Bolt, Garage&rsaquo;(1).FirstOrDefault();
````

The method uses Dijkstra’s shortest path algorithm to find the shortest path to the parent.
(Meaning the translated SQL has the lowest number of expensive joins!)

## Possible Exceptions
If the child table has a composite key, will throw a NotSupportedException.
If either the child or parent Types do not correspond to tables in the context model, will throw an ArgumentException.
