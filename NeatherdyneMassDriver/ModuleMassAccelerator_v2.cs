using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeatherdyneMassDriver
{
    [KSPModule("Mass Accelerator")]
    public class ModuleMassAccelerator_v2 : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public string LoadingpointName;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Accelerator force", guiUnits = "KiloNewtons")]
        public float acceleratorForce; //in kilonewtons

        public float MaxAcceleratorForce; //in kilonewtons


        [KSPField(isPersistant = false, guiActive = true, guiName = "Accelerator force", guiUnits = "KiloNewtons")]
        public float acceleratorDisplayForce; //in kilonewtons

#if true
        // float MaxAcceleratorForce { get { return MaxAcceleratorForce ; } }
#endif

        [KSPField(isPersistant = true, guiActive = true, guiName = "Power Level", guiUnits = "percent")]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float powerLevel = 100.0f;

        //[KSPField(isPersistant = false, guiActive = false)]
        //public float accelerationDuration;

        [KSPField(isPersistant = false, guiActive = false)]
        public float NetworkRange = 500.0f;


        [KSPField(isPersistant = false, guiActive = false)]
        public string armedAnimation;

        [KSPField(isPersistant = false, guiActive = false)]
        public float loadDistance = 2.0f; //in meters

        float LoadDistance { get { return loadDistance = 1.5f * (float)Math.Sqrt(totalSegmentHeight); } }

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

        [KSPField]
        public int segmentHeight = 1;

        internal ModuleMassAccelerator_v2 rootMA;
        int totalSegmentHeight = 0;
        Part topNode;
        AttachNode bottomAttachnode;

        public Guid LaunchTarget = Guid.Empty;

        public Vessel vLaunchTarget;
        internal double firetime = 999;
        bool firing = false;
        double countdownTime = 999;
        bool countdown = false;
        Transform subTransform = null;
        Vector3 position;

        public void Start()
        {
            if (this.part != null)
            {
                subTransform = this.part.FindModelTransform("LoadingPoint");
                position = subTransform.position;
            }
            bottomAttachnode = part.attachNodes.SingleOrDefault(m => m.id == "bottom");
            Events["DetachBottom"].guiActive = !(bottomAttachnode == null || bottomAttachnode.attachedPart == null);


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
                if (xanim.clip.name == name)
                {
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

        static int launcher = 0;
        double bottomMAHeight = 0;
        double topMAHeight = 0;
        int launcherId;
        void StartLaunch(int launcher, float powerLevel)
        {
            launcherId = launcher;

            firetime = 0;
            status = "firing";
            firing = true;
            countdown = false;
            this.powerLevel = powerLevel;

            bottomMAHeight = vessel.altitude - vessel.pqsAltitude;
            topMAHeight = bottomMAHeight + this.vessel.vesselSize.z;

            File.WriteAllText("massdriver - " + launcherId + ".csv", "MA Vessel.vesselsize," + vessel.vesselSize + Environment.NewLine);
            File.AppendAllText("massdriver-" + launcherId + ".csv", "vLaunchTarget.vesselsize," + vLaunchTarget.vesselSize + Environment.NewLine);

            File.AppendAllText("massdriver-" + launcherId + ".csv", "bottomMAHeight," + bottomMAHeight + Environment.NewLine);
            File.AppendAllText("massdriver-" + launcherId + ".csv", "topMAHeight.vesselsize," + topMAHeight + Environment.NewLine);

            string heading = "vesselBottomHeightAboveGround," +
                            "belowBottom," +
                            "vesselTopHeightAboveGround," +
                            "aboveTop," +
                            "percentBelowBottom," +
                            "percentAboveTop," +
                            "percentInMA," +
                            "f(kN)," +
                            "a(m/s^2)," +
                            "calcPower," +
                            "MA Mass," +
                            "Target Mass";
            File.AppendAllText("massdriver-" + launcherId + ".csv", heading + Environment.NewLine);

        }
        public void FixedUpdate()
        {
            double dt = TimeWarp.deltaTime;
            //should be 0 = realtime

            acceleratorDisplayForce = powerLevel / 100f * MaxAcceleratorForce;
            if (TimeWarp.CurrentRateIndex == 0 && armed && firing)
            {
                if (vLaunchTarget != null)
                {
                    var vespos = this.vessel.GetWorldPos3D();
                    var targetpos = vLaunchTarget.GetWorldPos3D();

                    Log.Info("++++++++++++++++++++++++++++++++++");
                    Log.Info("vesselpos " + vespos.ToString());
                    Log.Info("targetvesselpos " + targetpos.ToString());
                    Log.Info("localtransform " + subTransform.localPosition.ToString());
                    Log.Info("globaltransform " + subTransform.position.ToString());
                    Log.Info("parttransform " + transform.position.ToString());
                    Log.Info("distance from ship transform to part transform: " + Vector3.Distance(transform.position, vespos));

                    Log.Info("distance betwen them: " + Vector3.Distance(transform.position, subTransform.position).ToString());
                    //var accelerateVector = this.part.transform.position - vLaunchTarget.GetWorldPos3D();
                    var accelerateVector = (this.part.transform.position - position).normalized;
                    Log.Info("accelerate vector is " + accelerateVector.ToString());
                    //f = m/a
                    //f is in Kn
                    //m is in Mt (1000 kg)
                    //a is m/s^2
                    //a scales by DT
                    //a = f/m, assuming M is not 0

                    double vesselBottomHeightAboveGround = vLaunchTarget.altitude - vLaunchTarget.pqsAltitude;
                    double vesselTopHeightAboveGround = vesselBottomHeightAboveGround + vLaunchTarget.vesselSize.z;
                    double vesselHeight = vLaunchTarget.vesselSize.z;


                    if (vesselBottomHeightAboveGround < this.vessel.vesselSize.z + bottomMAHeight)
                    {
                        double calcPower = 1;


                        double percentBelowBottom = 0;
                        double belowBottom = (bottomMAHeight - vesselBottomHeightAboveGround);
                        if (belowBottom > 0)
                            percentBelowBottom = Math.Min(belowBottom, vesselHeight) / vesselHeight;

                        double percentAboveTop = 0;
                        double aboveTop = (vesselTopHeightAboveGround - topMAHeight);
                        if (aboveTop > 0)
                            percentAboveTop = Math.Min(aboveTop, vesselHeight) / vesselHeight;

                        double percentInMA = 1 - (percentBelowBottom / 2 + 2 * percentAboveTop);

                        calcPower = percentInMA;

                        double f = powerLevel / 100f * MaxAcceleratorForce * calcPower;
                        f = f * .5;
                        var spent = part.RequestResource(resourceName, dt * resourceAmount * f);
                        if (spent == 0)
                            f = 0;
                        double m = vLaunchTarget.RevealMass();
                        float aT = (float)(f / m);
                        vLaunchTarget.ChangeWorldVelocity(accelerateVector * (aT * (float)dt));
                        //for every action there is an equal and opposite reaction
                        m = this.vessel.RevealMass();
                        float aMA = (float)(f / m);
                        vLaunchTarget.ChangeWorldVelocity(accelerateVector * (-aMA * (float)dt));


                        string logline =
                            vesselBottomHeightAboveGround.ToString("F3") + "," +
                            belowBottom.ToString("F3") + "," +
                            vesselTopHeightAboveGround.ToString("F3") + "," +
                            aboveTop.ToString("F3") + "," +
                            percentBelowBottom.ToString("F3") + "," +
                            percentAboveTop.ToString("F3") + "," +
                            percentInMA.ToString("F3") + "," +
                            f.ToString("F3") + "," +
                            aT.ToString("F3") + "," +
                            calcPower.ToString("F3") + "," +
                            vessel.RevealMass().ToString("F3") + "," +
                            vLaunchTarget.RevealMass().ToString("F3");
                        File.AppendAllText("massdriver-" + launcherId + ".csv", logline + Environment.NewLine);
                    }
                }
                firetime += dt;
            }
            else
            {
                if (countdown)
                {
                    if (countdownTime < 0)
                    {
                        Log.Info("Fire 1");
                        Fire(powerLevel);
                        countdownTime = 999;
                        countdown = false;

                    }
                    else
                    {
                        status = string.Format("Time to launch: {0}", countdownTime.ToString("F1"));
                        countdownTime -= dt;
                    }
                }
            }
        }

        [KSPEvent(guiName = "Detach Bottom", guiActive = true)]
        void DetachBottom()
        {
            Log.Info("[Accelerator]: looking for node with id bottom");
            InitializeAllRoots(false);

            AttachNode attachnode = rootMA.part.attachNodes.SingleOrDefault(m => m.id == "bottom");

            if (attachnode != null && attachnode.attachedPart != null)
            {
                Log.Info("[Accelerator]: found attachnode bottom connected to " + attachnode.attachedPart.flightID);
                attachnode.attachedPart.decouple();
            }
            Events["DetachBottom"].guiActive = false;

        }

        float heightTopNode;
        Vector3 getCoM(Part part)
        {
            /* part.WCoM fails in the editor */
            return part.partTransform.position + part.partTransform.rotation * part.CoMOffset;
        }

        Vector3 GetPartSize(AvailablePart part)
        {
            List<Bounds> list = new List<Bounds>();
            if (!(part.partPrefab.Modules.GetModule<LaunchClamp>(0) != null))
            {

                Bounds[] partRendererBounds = PartGeometryUtil.GetPartRendererBounds(part.partPrefab);
                int num = partRendererBounds.Length;
                for (int j = 0; j < num; j++)
                {
                    Bounds bounds2 = partRendererBounds[j];
                    Bounds bounds3 = bounds2;
                    bounds3.size *= part.partPrefab.boundsMultiplier;
                    Vector3 size = bounds3.size;
                    bounds3.Expand(part.partPrefab.GetModuleSize(size, ModifierStagingSituation.CURRENT));
                    list.Add(bounds2);
                }
            }

            var pg = PartGeometryUtil.MergeBounds(list.ToArray(), part.partPrefab.transform.root).size;
            return pg;

        }
        public float GetTrueAlt()
        {
            //massAccelerators = this.vessel.FindPartModulesImplementing<ModuleMassAccelerator_v2>();
            float altitude = 0;
            //float d2max = 0;

            //foreach (var ma in massAccelerators)
            {

                //cnt++;
                //Log.Info("massAccelerator: " + cnt);
#if false
                foreach (AttachNode an in ma.part.attachNodes)
                {
                    var orgPos = ma.part.orgPos;
                    var CoM = an.position + orgPos; //  - vessel.GetWorldPos3D();
                   
                    Log.Info("orgPos: " + ma.part.orgPos + ", Topnode: " + part.topNode.position + ", an.position: " + an.position + ", vessel.GetWorldPos3D(): " + vessel.GetWorldPos3D() + ", CoM: " + CoM);


                    var up = (CoM - vessel.mainBody.position).normalized;
                    var ASL = (float)vessel.mainBody.GetAltitude(CoM);
                    float nodeAltitude;
                    if (Physics.Raycast(CoM, -up, out RaycastHit source, ASL + 10000, 1 << 15))
                    {
                        nodeAltitude = Mathf.Min(source.distance, ASL);
                    }
                    else
                    {
                        nodeAltitude = ASL;
                    }
                    Log.Info("Node altitude: " + nodeAltitude);
                    altitude = Math.Max(altitude, nodeAltitude);
                }
#endif
                //
                // Poor man's way to get top of vertical part:
                //
                // 1.  Get height of CoM of part, will be in the middle
                // 2.  Get size of part
                // 3.  Add 1/2 the vertical size of the part to the height obtained in #1
                //
                {
                    var CoM = getCoM(this.part);

                    var up = (CoM - this.vessel.mainBody.position).normalized;
                    var ASL = (float)this.vessel.mainBody.GetAltitude(CoM);

                    float nodeAltitude;
                    if (Physics.Raycast(CoM, -up, out RaycastHit source, ASL + 10000, 1 << 15))
                    {
                        // Log.Info("source.distance: " + source.distance);
                        nodeAltitude = Mathf.Min(source.distance, ASL);
                    }
                    else
                    {
                        nodeAltitude = ASL;
                    }
                    Vector3 moduleSize = GetPartSize(part.partInfo);
                    // Log.Info("vessel.terrainAltitude: " + vessel.terrainAltitude + ", ASL: " + ASL + ". CoM altitude: " + nodeAltitude + moduleSize.y / 2);
                    altitude = Math.Max(altitude, nodeAltitude + moduleSize.y / 2 + (float)vessel.terrainAltitude);
                }
#if false
                var d2 = (ma.part.partTransform.position - vessel.mainBody.position).magnitude - vessel.mainBody.Radius;
                d2max = (float)Math.Max(d2max, d2);
                Log.Info("d2Max: " + d2max);
#endif
            }

            return altitude;
        }

        static int numVesselsFound;
        void InitMaster(AcceleratorStack accelStack, bool arming)
        {
            //var CoM = vessel.findWorldCenterOfMass();

            if (!HighLogic.LoadedSceneIsEditor && status != "Armed")
            {
                heightTopNode = GetTrueAlt();

                MaxAcceleratorForce = acceleratorForce * totalSegmentHeight * HighLogic.CurrentGame.Parameters.CustomParams<IA>().powerLevel;
                //accelerationDuration = .5f * (float)Math.Sqrt(totalHeight);
                // loadDistance = 1.5f * (float)Math.Sqrt(totalHeight);

                Log.Info("totalHeight: " + totalSegmentHeight);
                Log.Info("acceleratorForce: " + MaxAcceleratorForce);
                //Log.Info("accelerationDuration: " + accelerationDuration);
                Log.Info("loadDistance: " + LoadDistance);

                if (master)
                {
                    Log.Info("finding closest target");
                    double cd = 0;
                    vLaunchTarget = getClosestVesselToLoadPoint(out cd, true);
                    if (vLaunchTarget != null)
                        numVesselsFound++;


                    if (vLaunchTarget != null)
                        Log.Info("found vessel nearby " + vessel.name + " dist = " + cd + " targetdist = " + LoadDistance);
                    if (vLaunchTarget != null && cd < LoadDistance)
                    {
                        Log.Info("found closest vessel...  " + vLaunchTarget.name + " dist " + cd.ToString());
                        armed = true;
#if false
                        foreach (BaseEvent e in Events)
                            Log.Info(e.name);
#endif
                        // LaunchTarget = vLaunchTarget.id;
                        foreach (var part in accelStack.members)
                        {
                            ModuleMassAccelerator_v2 massAccelerator = part.FindModuleImplementing<ModuleMassAccelerator_v2>();

                            massAccelerator.Events["StartCountdown"].guiActive = true;
                            massAccelerator.Events["Fire"].guiActive = true;
                            massAccelerator.Events["DisarmAccelerator"].guiActive = true;
                            massAccelerator.Events["ArmAccelerator"].guiActive = false;

                            massAccelerator.status = "Armed";
                            //massAccelerator.disarmTimeout = -1;
                            massAccelerator.countdown = false;
                            if (massAccelerator.armedAnimation != null)
                                PlayAnimation(massAccelerator.armedAnimation, false, false, false);
                        }
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
        }


        internal class AcceleratorStack
        {
            internal Part root;
            internal List<Part> members;

            internal AcceleratorStack(Part p)
            {
                root = p;
                members = new List<Part>();
                members.Add(p);
            }
        }
        static List<AcceleratorStack> networkRoots = new List<AcceleratorStack>();


        void InitializeAllRoots(bool arming = true)
        {
            Log.Info("InitializeAllRoots");
            networkRoots.Clear();
            numVesselsFound = 0;
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vector3d worldPos3D = FlightGlobals.Vessels[i].GetWorldPos3D();
                if (Vector3.Distance(position, worldPos3D) < NetworkRange)
                {
                    var v = FlightGlobals.Vessels[i];


                    var massAccelerators = v.FindPartModulesImplementing<ModuleMassAccelerator_v2>();
                    foreach (var ma in massAccelerators)
                    {
                        AttachNode an = ma.part.FindAttachNode("bottom");
                        if (an.attachedPart != null)
                        {
                            var massAccelerator = an.attachedPart.FindModuleImplementing<ModuleMassAccelerator_v2>();
                            if (massAccelerator == null)
                                networkRoots.Add(new AcceleratorStack(ma.part));
                        }
                        else
                            networkRoots.Add(new AcceleratorStack(ma.part));
                    }
                }
            }

            Log.Info("networkRoots.Count: " + networkRoots.Count);
            foreach (AcceleratorStack acelStack in networkRoots)
            {
                Part part = acelStack.root;
                ModuleMassAccelerator_v2 massAccelerator = part.FindModuleImplementing<ModuleMassAccelerator_v2>();
                ModuleMassAccelerator_v2 master = massAccelerator;
                master.topNode = master.part;
                master.master = true;
                master.rootMA = master;
                master.bottomAttachnode = rootMA.part.attachNodes.SingleOrDefault(m => m.id == "bottom");

                master.totalSegmentHeight = massAccelerator.segmentHeight;

                part = part.topNode.attachedPart;

                while (massAccelerator != null && part != null)
                {
                    massAccelerator = part.FindModuleImplementing<ModuleMassAccelerator_v2>();
                    if (massAccelerator != null)
                    {
                        acelStack.members.Add(part);
                        master.totalSegmentHeight += massAccelerator.segmentHeight;
                        master.topNode = massAccelerator.part;

                        massAccelerator.master = false;
                        massAccelerator.rootMA = master.rootMA;
                    }
                    part = part.topNode.attachedPart;
                }

                master.InitMaster(acelStack, arming);

            }
        }

        static LaunchWindow launchWindow = null;

        [KSPEvent(guiName = "Arm Accelerator", guiActive = true)]
        void ArmAccelerator()
        {
            Log.Info("ArmAccelerator");

            InitializeAllRoots();
            if (numVesselsFound > 0)
            {
                launchWindow = gameObject.AddComponent<LaunchWindow>();
                launchWindow.mma = this;
                launchWindow.SetXY(statusX, statusY);
                launchWindow.powerSetting = powerLevel;
                this.part.PartActionWindow.isValid = false;
            }
            else
                ScreenMessages.PostScreenMessage("No vessel positioned for launch", 10);

        }

        [KSPEvent(guiName = "Disarm Accelerator", guiActive = false)]
        internal void DisarmAccelerator()
        {
            foreach (var n in networkRoots)
            {
                foreach (var a in n.members)
                {
                    var massAccelerator = a.FindModuleImplementing<ModuleMassAccelerator_v2>();

                    massAccelerator.armed = false;
                    massAccelerator.status = "Idle";

                    massAccelerator.Events["Fire"].guiActive = false;
                    massAccelerator.Events["StartCountdown"].guiActive = false;
                    massAccelerator.Events["DisarmAccelerator"].guiActive = false;
                    massAccelerator.Events["ArmAccelerator"].guiActive = true;
                    if (massAccelerator.armedAnimation != null)
                        PlayAnimation(massAccelerator.armedAnimation, true, false, false);
                }
            }
            //disarmNetwork();
        }

        [KSPEvent(guiName = "Fire Accelerator", guiActive = false)]
        internal void Fire()
        {
            Fire(100f);
        }
        internal void Fire(float powerLevel)
        {
            launcher = 0;
            if (launchWindow != null)
                Destroy(launchWindow);
            foreach (var r in networkRoots)
            {
                var massAccelerator = r.root.FindModuleImplementing<ModuleMassAccelerator_v2>();

                var attachnode = rootMA.part.attachNodes.SingleOrDefault(m => m.id == "bottom");

                if (attachnode != null && attachnode.attachedPart != null)
                {
                    Log.Info("[Accelerator]: found attachnode bottom connected to " + attachnode.attachedPart.flightID);
                    attachnode.attachedPart.decouple();
                }
                launcher++;
                massAccelerator.StartLaunch(launcher, powerLevel);
            }
        }


        [KSPEvent(guiName = "Start Countdown", guiActive = false)]
        internal void StartCountdown()
        {
            Log.Info("StartCountdown");
            StartCountdown(HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownLength, powerLevel);
        }
        internal void StartCountdown(float countDownLength, float powerSetting)
        {
            Log.Info("StartCountdown, countDownLength: " + countDownLength + ", master: " + master);
            if (launchWindow != null)
                Destroy(launchWindow);

            //rootMA.StartCountdown();
            //rootMA.countdownTime = countDownLength;
            for (int i = 0; i < networkRoots.Count; i++)
            {
                var massAccelerator = networkRoots[i].root.FindModuleImplementing<ModuleMassAccelerator_v2>();
                //massAccelerator.StartCountdown();
                massAccelerator.countdownTime = countDownLength;
                massAccelerator.powerLevel = powerSetting;
                massAccelerator.armed = true;
                massAccelerator.countdown = true;
            }

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
                if (armed && countdown &&
                    HighLogic.CurrentGame.Parameters.CustomParams<IA>().countdownOnScreen &&
                    countdownTime > 0 &&
                    countdownTime <= 30)
                {
                    checkStatusLabelStyle();

                    Vector2 sizeTitle = statusLabelStyle.CalcSize(new GUIContent(status));
                    statusPos.Set(statusX, statusY, 200, sizeTitle.y);
                    DrawOutline(statusPos, status, 1, statusLabelStyle, Color.black, Color.white);
                }
#if false
                else
                {
                    if (disarmTimeout > 0) //enableShutdowntimeout)
                    {
                        checkStatusLabelStyle();

                        string disarmStatus = "Shutdown timeout: " + disarmTimeout.ToString("F1");
                        Vector2 sizeTitle = statusLabelStyle.CalcSize(new GUIContent(disarmStatus));
                        statusPos.Set(statusX, statusY, 240, sizeTitle.y);
                        DrawOutline(statusPos, disarmStatus, 1, statusLabelStyle, Color.black, Color.white);

                    }
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
}
