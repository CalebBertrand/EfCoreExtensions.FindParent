# FindParent
An entity framework core extension method to find a related parent entity any number of  tables (or joins) away.
Traverses the db context model to find the optimal join path, then dynamically generates a queryable of chained selects along the path.
Example:

Say there is a table structure where Garages have multiple Cars, Cars have multiple Tires, and Tires have multiple Bolts.
If you wanted to know the parent Garage of the Bolt with an Id of 1, you could get it via:

```
  Garage garageFromBolt = context.FindParent<Bolt, Garage>(1).FirstOrDefault();
```

The method uses Dijkstra’s shortest path algorithm to find the shortest path to the parent
(meaning the lowest number of required Navigation properties to Select).