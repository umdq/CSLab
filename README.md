# CSLab
my lab for compute shader:
* gpu sort
* boids simulation
* interactional water surface simulation(to be added-.-)
* fluid body simulation(to be added...)
## Boids Simulation
simulates the flocking behaviour of birds or fishes,[introduction](https://en.wikipedia.org/wiki/Boids)  
### my run result
(note low fps is because of my recording the video)
![](https://github.com/EagerCleaverInWind/CSLab/blob/master/CSLab/boids.gif)  
## GPU Sort
i.e. **bitonic sort,gpu friendly.**  
When it runS just by cpu,the time complexity is O(n*logn*logn).
However,if it's accelerated by gpu,because of the parallel computation,the time complexity can be saved to O(logn*logn)!  
  
![](https://github.com/EagerCleaverInWind/CSLab/blob/master/CSLab/20171106202552.png)
  
In this project,I implemented it to optimize the boids simulation.

