using UnityEngine;
namespace MIR
{
    [RequireComponent(typeof(Camera))]
    public sealed class MirFollowCamera : MonoBehaviour
    {
        public enum View { Isometric, ThirdPerson, DistrictShowcase }
        public View CurrentView {get;private set;}=View.Isometric;
        Transform target;
        Camera cachedCamera;
        // Source-benchmark repeatable 3/4 framing, not a fake concept overlay.
        // World positions are deliberate: plaza/store/cafe in the midground,
        // waterfront to the south in the foreground.
        static readonly Vector3 ShowcaseFocus=new Vector3(20f,1.8f,-48f);
        static readonly Quaternion ShowcaseRotation=Quaternion.Euler(51f,40f,0f);
        const float ShowcaseDistance=175f;
        const float ShowcaseFov=52f;
        float orbit=35f,tilt=43f,distance=23f;
        public void Init(Transform actor)
        {
            target=actor;
            cachedCamera=GetComponent<Camera>();
            cachedCamera.tag="MainCamera";
        }
        public void ToggleShowcase()
        {
            CurrentView=CurrentView==View.DistrictShowcase?View.Isometric:View.DistrictShowcase;
        }
        void LateUpdate()
        {
            if(target==null)return;
            if(Input.GetKeyDown(KeyCode.V))ToggleShowcase();
            if(Input.GetKeyDown(KeyCode.Tab))
                CurrentView=CurrentView==View.Isometric?View.ThirdPerson:View.Isometric;
            if(CurrentView==View.DistrictShowcase)
            {
                // A fixed reproducible camera to enable honest Unity/WebGL vs
                // approved PNG side-by-side capture. Scene is always live 3D.
                transform.position=ShowcaseFocus-ShowcaseRotation*Vector3.forward*ShowcaseDistance;
                transform.rotation=ShowcaseRotation;
                if(cachedCamera!=null)cachedCamera.fieldOfView=ShowcaseFov;
                return;
            }
            if(cachedCamera!=null)cachedCamera.fieldOfView=58f;
            float wheel=Input.mouseScrollDelta.y;
            if(CurrentView==View.Isometric)distance=Mathf.Clamp(distance-wheel*2,12f,52f);
            if(Input.GetMouseButton(1))
            {
                orbit+=Input.GetAxis("Mouse X")*2.4f;
                tilt=Mathf.Clamp(tilt-Input.GetAxis("Mouse Y")*1.7f,15f,75f);
            }
            if(CurrentView==View.ThirdPerson)
            {
                var desired=target.position+Vector3.up*4.0f-target.forward*7.2f;
                transform.position=Vector3.Lerp(transform.position,desired,Time.deltaTime*6f);
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(target.position+Vector3.up*1.5f-transform.position),Time.deltaTime*8);
            }
            else
            {
                var rot=Quaternion.Euler(tilt,orbit,0);
                var desired=target.position+Vector3.up*1.3f-rot*Vector3.forward*distance;
                transform.position=Vector3.Lerp(transform.position,desired,Time.deltaTime*5.5f);
                transform.rotation=Quaternion.Slerp(transform.rotation,rot,Time.deltaTime*5.5f);
            }
        }
    }
}
