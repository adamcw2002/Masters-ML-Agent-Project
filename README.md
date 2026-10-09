# An Evaluation of Reward Shaping Frameworks for Task-Oriented Reinforcement Learning in Procedurally Generated Environments using ML-Agents

**Adam Wright**<br>
Submitted for the degree of MSc by Research in Computer Science, University of Lincoln, 2026

Six frameworks are designed and evaluated in procedural kitchen environments inspired by Overcooked (Team17, 2016) using Unity ML-Agents.

<p align="center">
  <a href="https://youtu.be/2ZvDS6eM7Yc"><b>▶ Watch the overview video</b></a>
</p>

---

## Abstract

Reward shaping is a widely adopted technique in reinforcement learning (RL). It turns sparse or delayed task rewards into intermediate feedback that can help guide agent behaviour and improve learning efficiency. However, designing effective reward shaping usually requires expert knowledge and manual tuning. This study addresses that gap by designing and evaluating six reward shaping frameworks for task-oriented RL in procedurally generated environments. A custom Overcooked-inspired kitchen environment was built in Unity using the Unity ML-Agents package to train agents using the Proximal Policy Optimisation (PPO) algorithm across several recipe-based tasks of increasing complexity. Each framework was evaluated using the normalised final reward, sample efficiency, convergence speed and training stability. The results indicate that the severity of the penalty structure was the main factor that determined the learning performance. The low-penalty frameworks proved to be the most stable across all the recipes and conditions. On the other hand, frameworks relying on high completion rewards reintroduced the credit assignment problem and the aggressively penalised framework failed to learn any task at all. These findings indicate that reducing the penalty severity is more effective for stable learning than increasing the density of positive rewards. This study also provides design guidance for future work with reward shaping in procedurally generated task-oriented environments.



## Summary of Findings

The clearest finding of this study is that the severity of the penalty structure was the main contributing factor determining the learning performance across all six frameworks. The two frameworks with the lowest penalties, Exploration and Exploration+, achieved the highest average normalised rewards in both the non-delivery and delivery conditions.

* Exploration and Exploration+ were the only frameworks to record a positive normalised final reward in all seven conditions, and Speed was the only framework to record a negative value in all seven.
* Speed, which had the most aggressive penalty structure, was the only framework to never achieve a positive normalised reward in any condition, despite also having the largest positive reward for correct actions. The Speed framework converged after only 3.5 million steps to a permanent negative cumulative reward. It did not stabilise early because it had learned the task, but instead because it had stopped attempting it.
* The Planning framework, which had previously declined for more complex conditions was able to strongly recover and achieve a significant normalised reward, higher than the Exploration+ framework. This demonstrates that a strong completion reward can be effective in specific conditions, in this case when the agent had to differentiate between two recipes.



### Design Guidance for Task-oriented Reward Shaping

These recommendations apply to single-agent, long-horizon, recipe-structured tasks trained with PPO in PCG environments and their generality beyond this study is untested.

1. **Treat penalty severity as the primary design decision.** In the delivery conditions, ranking the frameworks by their penalty magnitude predicted their ranking by performance. Design effort may be better spent tuning the negative rewards instead of the positive ones.
2. **Make penalties informative instead of deterrent.** A penalty should be strong enough for the agent to distinguish an incorrect action from a correct one, and no larger.
3. **The magnitude of penalties should increase with the number of incorrect actions available.** Reducing the penalty severity improved the performance in every condition except the hybrid task where Exploration+ fell to a normalised final reward of 0.57 compared to Exploration achieving 0.74.
4. **Distribute rewards across the task instead of concentrating it at completion.** Frameworks with large terminal rewards performed well on short tasks and declined as the task length extended, since the completion condition was reached less often.
5. **A large completion reward can be effective for short tasks where the goal is unclear.** Planning scored 0.68 in the hybrid condition and converged the fastest of the six frameworks while the Base and precision dropped into the negatives.
6. **Uniform weighting can perform comparably to manual positive reward tuning.** The Base framework applied the same value to every reward signal, requiring no tuning and was not outperformed by either Precision or Planning in any delivery condition.
7. **Test the reward structure for exploitation before full training.** Every reward hacking behaviour was identified during the preliminary testing phase and resolved before mass training began. This saved a substantial amount of time given the training time took roughly 400 hours in total across all runs.

