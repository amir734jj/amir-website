## Background  
*I work on Service Fabric, and it's been an amazing experience. Every day feels like we're making a real, positive impact for our customers. It's genuinely a privilege to work in this environment.*

Service Fabric is used primarily within Microsoft. On the surface, it may look similar to Kubernetes, but in reality, it's quite different. Kubernetes is mainly a container orchestration tool, whereas Service Fabric is closer to a highly scalable distributed platform that also supports running containers.  

Service Fabric supports both stateless and stateful services. Stateless services (like reverse proxies or external-facing APIs) do not store data—if they fail, new instances are simply created. Stateful services, on the other hand, persist data to disk, so when an instance fails, it is recreated with its data intact. Stateful services can be either volatile or non-volatile, depending on how data persistence is handled.  

There are also key concepts such as replicas, partitions, services, and applications, along with platform-level features like repair and health management. There is a steep learning curve to effectively support Service Fabric customers, but once you build that foundation, it becomes much more intuitive.

## Experience is Key  

When we join a customer call, we are expected to act as SMEs. That means understanding the system deeply, knowing our limits, and knowing who to reach out to when needed.  

Time pressure is a major factor. Customers expect answers in real time, and there's often little opportunity to pause and research during a call. This can be stressful, but it becomes more manageable with experience.

## Types of Incidents  

We typically encounter several categories of incidents:  

- Known issues (low to medium difficulty): Problems we have seen before or can quickly identify. These are the least stressful.  
- Unrelated issues: Cases where the root cause is outside Service Fabric, even if it initially appears related.  
- Unknown Service Fabric issues: We know SF is involved, but the root cause isn't clear. These require deeper investigation and are difficult to resolve during a live call. These can be quite stressful.  
- Outages: Everything is down, including Service Fabric. These are high-pressure, all-hands-on-deck situations.

## What I have Learned  

Stay calm and composed, panic doesn't help. Confidence is important, but it needs to be balanced with caution. Always verify instructions before sharing them with customers.  

Providing incorrect guidance can have consequences: at best, it prolongs the call; at worst, it can make the issue worse.  

I have also learned to maintain a well-organized set of documentation for investigations and to build a mental map of where to find critical information quickly.
