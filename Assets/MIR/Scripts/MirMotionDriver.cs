using System;
using UnityEngine;

namespace MIR
{
    public sealed class MirMotionDriver : MonoBehaviour
    {
        Animator animator;
        MirFallbackWalker proxy;
        MirActivity current=MirActivity.Idle;
        float actionEnd;
        string lastState="";
        float personality;
        public void Configure(float gaitSeed){personality=Mathf.Clamp01(gaitSeed);animator=GetComponentInChildren<Animator>();proxy=GetComponent<MirFallbackWalker>();
            if(animator!=null){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}}
        public void Tick(float speed,MirActivity action,bool moving=true)
        {
            // Travel distance/road collision are controlled by character movement, never by the FBX root curve.
            if(animator==null && proxy==null)Configure(personality);
            float normalizedSpeed=Mathf.Clamp01(speed/5.5f);
            if(proxy!=null){proxy.Pose(normalizedSpeed,Time.time,action);return;}
            if(animator==null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController==null)return;
            animator.speed=.92f+personality*.15f;
            animator.SetFloat("Speed",normalizedSpeed,.15f,Time.deltaTime);
            string target=personality>.69f?"Locomotion_Formal":"Locomotion";
            if(!moving || normalizedSpeed<.08f)
            {
                switch(action){
                    case MirActivity.Talking:target="MIR_Talk";break;
                    case MirActivity.Phone:target="MIR_Phone";break;
                    case MirActivity.Sitting:target="MIR_Sit";break;
                    case MirActivity.Driving:target="MIR_Drive";break;
                    case MirActivity.Working:target="MIR_Work";break;
                    case MirActivity.Harvesting:target="MIR_Harvest";break;
                    case MirActivity.Eating:target="MIR_Eat";break;
                    default:break;
                }
            }
            if(target!=lastState)
            {
                // Exit from action to locomotion with a short cross-fade (prevents stuck upper arms/T-poses).
                if(animator.HasState(0,Animator.StringToHash(target)))
                    animator.CrossFadeInFixedTime(target,.2f,0);
                else animator.CrossFadeInFixedTime("Locomotion",.2f,0);
                lastState=target;
            }
            current=action;
        }
        public void ForceReset(){lastState="";}
    }
}
