﻿using System;
using System.Collections;
using KSP.IO;
using System.Collections.Generic;
using UnityEngine;
using kOS.Context;
using kOS.Persistance;

namespace kOS.Module
{
    public class kOSProcessor : PartModule, IProcessorModule
    {
        private ICPU cpu;
        private int vesselPartCount;
        private readonly List<IProcessorModule> sisterProcs = new List<IProcessorModule>();
        private const int MEM_SIZE = 10000;

        public IVolume HardDisk { get; private set; }

        [KSPEvent(guiActive = true, guiName = "Open Terminal")]
        public void Activate()
        {
            Core.OpenWindow(cpu);
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            if (cpu == null) return;

            cpu.Mode = cpu.Mode != CPUMode.OFF ? CPUMode.OFF : CPUMode.STARVED;
        }

        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param) {
            Activate();
        }

        [KSPAction("Close Terminal", actionGroup = KSPActionGroup.None)]
        public void Deactivate(KSPActionParam param) {
            Core.CloseWindow(cpu);
        }

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        public void TogglePower(KSPActionParam param) {
            TogglePower();
        }

        [KSPField(isPersistant = true, guiName = "kOS Unit ID", guiActive = true, guiActiveEditor = true)]
        private int unitID = -1;

        [KSPEvent(guiName = "Unit +", guiActive = false, guiActiveEditor = true)]
        public void IncrementUnitId() { unitID++; cpu.UpdateUnitId(unitID); }

        [KSPEvent(guiName = "Unit -", guiActive = false, guiActiveEditor = true)]
        public void DecrementUnitId() { unitID--; cpu.UpdateUnitId(unitID);}


        [KSPField(isPersistant = true, guiActive = false)]
        public int MaxPartID = 100;

        public override void OnStart(StartState state)
        {
            //Do not start from editor and at KSP first loading
            if (state == StartState.Editor || state == StartState.None)
            {
                return;
            }

            if (HardDisk == null) HardDisk = new Harddisk(MEM_SIZE);

            InitCpu();

        }

        public void InitCpu()
        {
            if (cpu != null) return;
            cpu = new CPU(this, "ksp");
            cpu.AttachVolume(HardDisk);
            cpu.Boot();
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            UnityEngine.Debug.Log("*** External Function Registration Succeeded");

            cpu.RegisterkOSExternalFunction(parameters);
        }
        
        public static int AssignNewID()
        {
            var config = PluginConfiguration.CreateForType<IProcessorModule>();
            config.load();
            var id = config.GetValue<int>("CpuIDMax") + 1;
            config.SetValue("CpuIDMax", id);
            config.save();

            return id;
        }
        
        public void Update()
        {
            if (cpu == null) return;

            if (part.State == PartStates.DEAD)
            {
                cpu.Mode = CPUMode.OFF;
                return;
            }

            cpu.Update(Time.deltaTime);

            cpu.ProcessElectricity(part, TimeWarp.fixedDeltaTime);

            UpdateParts();
        }

        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking)
            if (vessel.parts.Count == vesselPartCount) return;

            var attachedVolumes = new List<IVolume> {cpu.Archive, HardDisk};

            // Look for sister units that have newly been added to the vessel
            sisterProcs.Clear();
            foreach (var item in vessel.parts)
            {
                IProcessorModule sisterProc;
                if (item == part || !PartIsKosProc(item, out sisterProc)) continue;

                sisterProcs.Add(sisterProc);
                attachedVolumes.Add(sisterProc.HardDisk);
            }

            cpu.UpdateVolumeMounts(attachedVolumes);

            vesselPartCount = vessel.parts.Count;
        }

        public bool PartIsKosProc(Part input, out IProcessorModule proc)
        {
            foreach (PartModule module in input.Modules)
            {
                var processor = module as IProcessorModule;
                if (processor == null) continue;
                proc = processor;
                return true;
            }

            proc = null;
            return false;
        }

        public override void OnFixedUpdate()
        {
            
        }

        public override void OnLoad(ConfigNode node)
        {
            // KSP Seems to want to make an instance of my partModule during initial load
            if (vessel == null) return;

            foreach (var hdNode in node.GetNodes("harddisk"))
            {
                var newDisk = new Harddisk(hdNode);
                HardDisk = newDisk;
            }

            UnityEngine.Debug.Log("******************************* ON LOAD ");

            InitCpu();

            UnityEngine.Debug.Log("******************************* CPU Inited ");

            if (cpu != null) cpu.OnLoad(node);
            
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            if (HardDisk != null)
            {
                var hdNode = HardDisk.Save("harddisk");
                node.AddNode(hdNode);
            }

            if (cpu != null)
            {
                cpu.OnSave(node);
            }

            base.OnSave(node);
        }
    }
}
