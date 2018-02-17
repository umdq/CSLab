# CSLab
my lab for compute shader:
* gpu sort
* boids simulation
* interactional water surface simulation(to be added-.-)
* fluid body simulation(to be added...)
## GPU Sort
i.e. **bitonic sort,gpu friendly.**  
When it runS just by cpu,the time complexity is O(n*logn*logn).
However,if it's accelerated by gpu,because of the parallel computation,the time complexity can be saved to O(logn*logn)!  
  
![](https://github.com/EagerCleaverInWind/CSLab/blob/master/CSLab/20171106202552.png)
  
In this project,I implemented it to optimize the boids simulation.
## Boids Simulation
It simulates the flocking behaviour of birds or fishes,[introduction](https://en.wikipedia.org/wiki/Boids) 
### Optimization
The easiest way to perform the simulation is with a simple N2 search. The compute shader will execute one thread per entity and iterate over all other entities looking for neighbors within the desired search radius. However,even using GPU,this technique is extremely expensive since we only care about entities within a relatively small neighboring area.So I try to optimize it with the spatial grid based on GPU bitonic sort,which accounts for a greater proportion of code work than the boids simulation.  
### Rendering
In the rendering pass,note that the entity is not a point but a mesh.After all,the entities of this simulation are not particles as usual.As a result,besides translation,we have to apply rotation to the entities.On the CPU i.e. in unity,it's easy to apply the rotation described by a quaternion.But on the GPU,no lab of quaternion,so I have to implement it from from scratch.
### Run Result
(note low fps is because of my recording the video...)
![](https://github.com/EagerCleaverInWind/CSLab/blob/master/CSLab/boids.gif)
