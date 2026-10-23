using UnityEngine;

namespace MIR
{
    public sealed class MirMotionDriver : MonoBehaviour
    {
        // Mirrors the speeds used by MirNpcAgent and MirPlayer; the generated
        // 1D BlendTree has idle=0, walk=.42 and run=1 thresholds.
        // This maps BOTH NPC and player walking to full walk, not an idle/walk mix.
        const float NpcWalkSpeed=1.3f;
        const float PlayerWalkSpeed=2.7f;
        const float PlayerRunSpeed=5.5f;
        const float WalkBlend=.42f;

        Animator animator;
        MirFallbackWalker proxy;
        string lastState="";
        float personality;

        static float LocomotionParameter(float metresPerSecond)
        {
            float speed=Mathf.Max(0f,metresPerSecond);
            if(speed<NpcWalkSpeed)return speed/NpcWalkSpeed*WalkBlend;
            if(speed<=PlayerWalkSpeed)return WalkBlend;
            return Mathf.Lerp(WalkBlend,1f,Mathf.Clamp01(
                (speed-PlayerWalkSpeed)/(PlayerRunSpeed-PlayerWalkSpeed)));
        }

        public void Configure(float gaitSeed)
        {
            personality=Mathf.Clamp01(gaitSeed);
            animator=GetComponentInChildren<Animator>();
            proxy=GetComponent<MirFallbackWalker>();
            if(animator!=null)
            {
                animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            }
        }

        public void Tick(float speed,MirActivity action,bool moving=true)
        {
            // World movement is driven by the player/NPC, never by animation root curves.
            if(animator==null && proxy==null)Configure(personality);
            float blend=LocomotionParameter(moving?speed:0f);
            if(proxy!=null){proxy.Pose(blend,Time.time,action);return;}
            if(animator==null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController==null)return;

            animator.speed=.92f+personality*.15f;
            animator.SetFloat("Speed",blend,.15f,Time.deltaTime);

            string target=action==MirActivity.Carrying?"Locomotion_Carry":
                personality>.69f?"Locomotion_Formal":"Locomotion";
            if(!moving || blend<.08f)
            {
                switch(action)
                {
                    case MirActivity.Talking:target="MIR_Talk";break;
                    case MirActivity.Phone:target="MIR_Phone";break;
                    case MirActivity.Sitting:target="MIR_Sit";break;
                    case MirActivity.Driving:target="MIR_Drive";break;
                    case MirActivity.Working:target="MIR_Work";break;
                    case MirActivity.Harvesting:target="MIR_Harvest";break;
                    case MirActivity.Eating:target="MIR_Eat";break;
                }
            }

            // Remember the state actually requested from Animator, not a missing
            // action state. Avoid repeated CrossFades when an optional clip is absent.
            if(target!=lastState)
            {
                string resolved=animator.HasState(0,Animator.StringToHash(target))?
                    target:"Locomotion";
                if(resolved!=lastState && animator.HasState(0,Animator.StringToHash(resolved)))
                {
                    animator.CrossFadeInFixedTime(resolved,.2f,0);
                    lastState=resolved;
                }
            }
        }
        public void ForceReset(){lastState="";}
    }
}
