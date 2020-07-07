using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeatherdyneMassDriver
{
    [KSPModule("Mass Accelerator")]
    public class ModuleMassAccelerator : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public string LoadingpointName;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Accelerator force", guiUnits = "KiloNewtons")]
        public float acceleratorForce; //in kilonewtons

        [KSPField(isPersistant = false, guiActive = true, guiName = "Accelerator force", guiUnits = "KiloNewtons")]
        public float acceleratorDisplayForce; //in kilonewtons

#if true
        float AcceleratorForce { get { return acceleratorForce * HighLogic.CurrentGame.Parameters.CustomParams<IA>().powerLevel; } }
#endif

        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Level", guiUnits = "percent")]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float powerLevel = 100.0f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float accelerationDuration;

        [KSPField(isPersistant = false, guiActive = false)]
        public float NetworkRange = 500.0f;


        [KSPField(isPersistant = false, guiActive = false)]
        public string armedAnimation;

        [KSPField(isPersistant = false, guiActive = false)]
        public float loadDistance = 2.0f; //in meters

        [KSPField(isPersistant = true, guiActive = true, guiName = "Status")]
        public string status;

        //  [KSPField(isPersistant = true, guiActive = false)]
        public bool armed = false; //when armed is set to true by the event the vessel inside the load point zone will be "loaded", brought to the center of the magnet, and oriented up

        [KSPField(isPersistant = false, guiActive = false)]
        public bool master = false;

        [KSPField(isPersistant = false, guiActive = false)]
        public float resourceAmount;
        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceName;
        //[KSPField(isPersistant = true, guiActive = false)]
        public Guid LaunchTarget = Guid.Empty;

        public Vessel vLaunchTarget;
        double firetime = 999;
        double disarmTimeout = -1;
        bool enableShutdowntimeout = false;
        bool firing = false;
        double countdownTime = 999;
        bool countdown = false;
        List<ModuleMassAccelerator> massAccelerators = null;
        Transform subTransform = null;
        Vector3 position;
        public void Start()
        {
            Log.Info("ModuleMassAccelerator.Start");
            if (!master)
                Fields["status"].guiActive = false;
            if (!master)
            {
                Events["StartCountdown"].guiActive = false;
                Events["Fire"].guiActive = false;
                Events["DisarmAccelerator"].guiActive = false;
                Events["ArmAccelerator"].guiActive = false;

            }

            {
                massAccelerators = this.vessel.FindPartModulesImplementing<ModuleMassAccelerator>();
                Log.Info("Network found modules " + massAccelerators.Count.ToString());
                for (int i = massAccelerators.Count - 1; i > 0; i--)
                {
                    if (massAccelerators[i].master)
                        massAccelerators.Remove(massAccelerators[i]);
                }

            }
            subTransform = this.part.FindModelTransform("LoadingPoint");
            position = subTransform.position;
        }

        void ArmNetwork(Vessel v)
        {
            //Log.Info("Network found modules " + massAccelerators.Count.ToString());
            for (int i = 0; i < massAccelerators.Count; i++)
                massAccelerators[i].ArmAccelerator();
        }

        void DisarmAcceleratorTimeout()
        {
            if (countdownTime < HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength)
            {
                enableShutdowntimeout = true;
                disarmTimeout = HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength;
            }
            armed = false;
            //countdownTime = 999;
        }

        void DisarmNetwork(Vessel v)
        {
            //List<ModuleMassAccelerator> massAccelerators = v.FindPartModulesImplementing<ModuleMassAccelerator>();
            for (int i = 0; i < massAccelerators.Count; i++)
                massAccelerators[i].DisarmAcceleratorTimeout();
        }

        private void armNetwork()
        {
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vector3d worldPos3D = FlightGlobals.Vessels[i].GetWorldPos3D();
                if (Vector3.Distance(position, worldPos3D) < NetworkRange)
                {
                    ArmNetwork(FlightGlobals.Vessels[i]);
                }
            }
        }

        private void disarmNetwork()
        {
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vector3d worldPos3D = FlightGlobals.Vessels[i].GetWorldPos3D();
                if (Vector3.Distance(position, worldPos3D) < NetworkRange)
                {
                    DisarmNetwork(FlightGlobals.Vessels[i]);
                }
            }
        }

        private void PlayStartAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName != "")
            {
                if (speed < 0)
                {
                    StartAnimation[startAnimationName].time = StartAnimation[startAnimationName].length;
                }
                if (instant)
                    StartAnimation[startAnimationName].speed = 999999 * speed;
                StartAnimation[startAnimationName].speed = speed;
                StartAnimation.Play(startAnimationName);
            }
        }

        public void PlayAnimation(string name, bool rewind, bool instant, bool loop)
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing. That's cool, I called in the part.cfg

            var anim = part.FindModelAnimators();

            for (int i = 0; i < anim.Count(); i++)
            {
                Animation xanim = anim[i];
                // Log.Info("animation found " + a.name + " " + a.clip.name);
                if (xanim.clip.name == name)
                {
                    // Log.Info("animation playingxx " + a.name + " " + a.clip.name);
                    if (loop)
                        xanim.wrapMode = WrapMode.Loop;
                    else
                        xanim.wrapMode = WrapMode.Once;
                    PlayStartAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                }
            }
        }

        Vessel getLaunchTarget()
        {
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                if (FlightGlobals.Vessels[i].id == LaunchTarget)
                    return FlightGlobals.Vessels[i];
            }
            return null;
        }

        Vessel getClosestVesselToLoadPoint(out double distance)
        {
            return getClosestVesselToLoadPoint(out distance, false);
        }

        Vessel getClosestVesselToLoadPoint(out double distance, bool debug)
        {
            if (debug)
                Log.Info(subTransform.name);
            var testlocation = subTransform.transform.position;

            var accelerateVector = this.part.transform.position - testlocation;
            if (debug)
                Log.Info("accelerate vector = " + accelerateVector.ToString());
            var location = subTransform.transform.position;
            if (debug)
                Log.Info("Getting closest vessel... locations " + testlocation.ToString() + " vesselloc " + location.ToString());
            Vessel closest = null;
            double closestDistance = 2300; //any further than this and it doesn't matter
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                if (FlightGlobals.Vessels[i].id != this.vessel.id)
                {
                    Vector3d targetLocation = FlightGlobals.Vessels[i].GetWorldPos3D();
                    double d = Vector3d.Distance(location, targetLocation);
                    if (debug)
                        Log.Info("vessel test: " + FlightGlobals.Vessels[i].name + " loc = " + targetLocation.ToString() + " distance = " + d.ToString());
                    if (d < closestDistance)
                    {
                        closest = FlightGlobals.Vessels[i];
                        closestDistance = d;
                    }
                }

            }

            distance = closestDistance;
            return closest;
        }

        public override void OnActive()
        {

            base.OnActive();
        }

        void pullLaunchTargetToLaunchPosition(float dt)
        {
            if (vLaunchTarget != null)
            {
                var pullVector = position - vLaunchTarget.GetWorldPos3D();
                //  Log.Info("pull vector " + pullVector.ToString());
                // Log.Info("acceleration " + (pullVector * dt).ToString());
                double f = powerLevel * AcceleratorForce / 100;
                var spent = part.RequestResource(resourceName, dt * resourceAmount * f);
                if (spent == 0)
                {
                    f = 0;
                }

                double m = vLaunchTarget.RevealMass();
                float a = (float)(f / m);
                double strength = 4 * (pullVector.magnitude - loadDistance) / (loadDistance) * a;
                pullVector.Normalize();
                pullVector = pullVector * a;
                vLaunchTarget.ChangeWorldVelocity(pullVector * dt);
                m = this.vessel.RevealMass();
                a = (float)(f / m);
                pullVector.Normalize();
                pullVector = pullVector * -a; //for every action there is an equal and opposite reaction
                                              //this.vessel.ChangeWorldVelocity(pullVector);
                                              //calculate resource draw based on the applied power


            }
        }

        public override void OnUpdate()
        {
            double dt = TimeWarp.deltaTime;
            //should be 0 = realtime

            acceleratorDisplayForce = powerLevel * AcceleratorForce / 100;
            if (TimeWarp.CurrentRateIndex == 0 && armed)
            {
                if (firetime < accelerationDuration)
                {
                    if (vLaunchTarget != null)
                    {
                        var vespos = this.vessel.GetWorldPos3D();
                        var targetpos = vLaunchTarget.GetWorldPos3D();
                        Log.Info("vesselpos " + vespos.ToString());
                        Log.Info("targetvesselpos " + targetpos.ToString());
                        Log.Info("localtransform " + subTransform.localPosition.ToString());
                        Log.Info("globaltransform " + subTransform.position.ToString());
                        Log.Info("parttransform " + transform.position.ToString());
                        Log.Info("distance from ship transform to part transform" + Vector3.Distance(transform.position, vespos));

                        Log.Info("distance betwen them" + Vector3.Distance(transform.position, subTransform.position).ToString());
                        //var accelerateVector = this.part.transform.position - vLaunchTarget.GetWorldPos3D();
                        var accelerateVector = (this.part.transform.position - position).normalized;
                        Log.Info("accelerate vector is " + accelerateVector.ToString());
                        //f = m/a
                        //f is in Kn
                        //m is in Mt (1000 kg)
                        //a is m/s^2
                        //a scales by DT
                        //a = f/m, assuming M is not 0


                        double f = powerLevel * AcceleratorForce / 100f;
                        var spent = part.RequestResource(resourceName, dt * resourceAmount * f);
                        if (spent == 0)
                            f = 0;
                        double m = vLaunchTarget.RevealMass();
                        float a = (float)(f / m);
                        dt = Math.Min(dt, accelerationDuration); //for slow computers... don't apply more than the duration of acceleration
                        vLaunchTarget.ChangeWorldVelocity(accelerateVector * (a * (float)dt));
                        //for every action there is an equal and opposite reaction
                        m = this.vessel.RevealMass();
                        a = (float)(f / m);
                        vLaunchTarget.ChangeWorldVelocity(accelerateVector * (-a * (float)dt));

                    }
                    firetime += dt;
                }
                else if (firing)
                {
                    armed = false;
                    status = "idle";
                    firing = false;

                    DisarmAccelerator();
                }

                if (!firing)
                {
                    if (!master)
                        pullLaunchTargetToLaunchPosition((float)dt);

                }

                if (countdown)
                {
                    if (countdownTime < 0)
                    {
                        Log.Info("Fire 1");
                        Fire();
                        countdownTime = 999; //  HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength;
                        countdown = false;

                    }
                    else
                    {
                        status = string.Format("Time to launch: {0}", countdownTime.ToString("F1"));
                        countdownTime -= dt;
                    }


                }

                if (!master)
                {
                    double dist = 0;
                    vLaunchTarget = getClosestVesselToLoadPoint(out dist);
                    if (vLaunchTarget && dist < loadDistance)
                    {
                        Log.Info("Fire 2, dist: " + dist + ", loadDistance: " + loadDistance);
                        Fire();
                    }
                    else
                        vLaunchTarget = null;

                    if (enableShutdowntimeout)
                    {
                        disarmTimeout -= dt;
                        if (disarmTimeout < 0)
                        {
                            enableShutdowntimeout = false;
                            DisarmAccelerator();
                        }
                    }
                }
            }
            else
            {
                if (master && enableShutdowntimeout)
                {
                    disarmTimeout -= dt;
                    if (disarmTimeout < 0)
                    {
                        enableShutdowntimeout = false;
                        //DisarmAccelerator();
                    }
                }

            }

            base.OnUpdate();
        }

#if unknown
        [KSPEvent(guiName = "Detach Bottom", guiActive = true)]
        void DetachIdTop()
        {
            Log.Info("[Accelerator]: looking for node with id chamber");
            var attachnode = part.attachNodes.SingleOrDefault(m => m.id == "chamber");
            if (attachnode != null && attachnode.attachedPart != null)
            {
                Log.Info("[Accelerator]: found attachnode chamber connected to " + attachnode.attachedPart.flightID);
                attachnode.attachedPart.decouple();
            }
        }
#endif

        [KSPEvent(guiName = "Arm Accelerator", guiActive = true)]
        void ArmAccelerator()
        {

            double cd = 0;
            vLaunchTarget = getClosestVesselToLoadPoint(out cd, true);
            Log.Info("finding closest target");
            if (master)
            {
                if (vLaunchTarget != null)
                    Log.Info("found vessel nearby " + vessel.name + " dist = " + cd + " targetdist = " + loadDistance);
                if (vLaunchTarget != null && cd < loadDistance)
                {
                    Log.Info("found closest vessel...  " + vLaunchTarget.name + " dist " + cd.ToString());
                    armed = true;
#if DEBUG
                    foreach (BaseEvent e in Events)
                        Log.Info(e.name);
#endif
                    // LaunchTarget = vLaunchTarget.id;
                    status = "Armed";
                    Events["StartCountdown"].guiActive = true;
                    Events["Fire"].guiActive = true;
                    Events["DisarmAccelerator"].guiActive = true;
                    Events["ArmAccelerator"].guiActive = false;
                    disarmTimeout = -1;
                    countdown = false;

                    PlayAnimation(armedAnimation, false, false, false);
                }
                else
                    vLaunchTarget = null;
            }
            else //if (!master)
            {
                PlayAnimation(armedAnimation, false, false, false);
                armed = true;
                status = "Armed";
            }
        }

        [KSPEvent(guiName = "Disarm Accelerator", guiActive = false)]
        void DisarmAccelerator()
        {
            armed = false;
            status = "Idle";

            Events["Fire"].guiActive = false;
            Events["StartCountdown"].guiActive = false;
            Events["DisarmAccelerator"].guiActive = false;
            Events["ArmAccelerator"].guiActive = true;
            PlayAnimation(armedAnimation, true, false, false);
            disarmNetwork();
        }

        [KSPEvent(guiName = "Fire Accelerator", guiActive = false)]
        void Fire()
        {
            armNetwork();
            firetime = 0;
            status = "firing";
            firing = true;
        }

        [KSPEvent(guiName = "Start Countdown", guiActive = false)]
        void StartCountdown()
        {
            Events["StartCountdown"].guiActive = false;
            countdownTime = HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength;
            countdown = true;
        }


        private GUIStyle statusLabelStyle;
        [Persistent]
        float statusX = Screen.width * 0.75f;
        [Persistent]
        float statusY = 100;
        [Persistent]
        int timeSize = 20;

        private const int LEFT = 10;
        private const int TOP = 300;
        private const int WIDTH = 120;
        private const int HEIGHT = 50;

        private Rect statusPos = new Rect(LEFT, TOP, WIDTH, HEIGHT);

        void checkStatusLabelStyle()
        {
            if (statusLabelStyle == null)
            {
                statusLabelStyle = new GUIStyle(GUI.skin.label);
                statusX = Mathf.Clamp(statusX, 0, Screen.width);
                statusY = Mathf.Clamp(statusY, 0, Screen.height);
                statusLabelStyle.fontSize = timeSize;

            }
        }
        public void OnGUI()
        {
            if (master)
            {
                if (armed && HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownOnScreen && countdownTime > 0 && countdownTime <= HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength)
                {
                    checkStatusLabelStyle();

                    Vector2 sizeTitle = statusLabelStyle.CalcSize(new GUIContent(status));
                    statusPos.Set(statusX, statusY, 200, sizeTitle.y);
                    DrawOutline(statusPos, status, 1, statusLabelStyle, Color.black, Color.white);
                }
                else
                if (disarmTimeout > 0) //enableShutdowntimeout)
                {
                    checkStatusLabelStyle();

                    string disarmStatus = "Shutdown timeout: " + disarmTimeout.ToString("F1");
                    Vector2 sizeTitle = statusLabelStyle.CalcSize(new GUIContent(disarmStatus));
                    statusPos.Set(statusX, statusY, 240, sizeTitle.y);
                    DrawOutline(statusPos, disarmStatus, 1, statusLabelStyle, Color.black, Color.white);

                }
#if false
                else
                {
                    checkStatusLabelStyle();

                    string disarmStatus = " ";
                    Vector2 sizeTitle = statusLabelStyle.CalcSize(new GUIContent(disarmStatus));
                    statusPos.Set(statusX, statusY, 240, sizeTitle.y);
                    DrawOutline(statusPos, disarmStatus, 1, statusLabelStyle, Color.black, Color.white);


                }
#endif
            }
        }
        void DrawOutline(Rect r, string t, int strength, GUIStyle style, Color outColor, Color inColor)
        {
            Color backup = style.normal.textColor;
            style.normal.textColor = outColor;
            for (int i = -strength; i <= strength; i++)
            {
                GUI.Label(new Rect(r.x - strength, r.y + i, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + strength, r.y + i, r.width, r.height), t, style);
            }
            for (int i = -strength + 1; i <= strength - 1; i++)
            {
                GUI.Label(new Rect(r.x + i, r.y - strength, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + i, r.y + strength, r.width, r.height), t, style);
            }
            style.normal.textColor = inColor;
            GUI.Label(r, t, style);
            style.normal.textColor = backup;
        }

        public override string GetInfo()
        {
            return "";
        }
    }

    //public class automatedConstruction: ExLP.ExWorkshop
    //{

    //    [KSPField(guiActive = false, isPersistant=true)]
    //    public float automatedProductivity = 20.0f;
    //    public override void OnUpdate()
    //    {
    //        base.OnUpdate();
    //        Productivity = automatedProductivity;
    //    }


    //}
}
