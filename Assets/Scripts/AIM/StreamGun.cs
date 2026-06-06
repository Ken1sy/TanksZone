using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

namespace GameScripts.AIM
{
    public enum StreamType { Firebird, Freeze }

    public class StreamGun : WeaponController
    {
        [Header("Stream Type Settings")]
        public StreamType type = StreamType.Firebird;
        public float temperaturePerTick = 10f;
        public float tickRate = 0.25f;
        [Header("Energy System")]
        public float maxEnergy = 100f;
        public float energyDrainRate = 25f;
        public float energyRechargeRate = 15f;
        [Header("Visual Effects")]
        public ParticleSystem streamParticles;
        public AudioSource streamAudio;

        private bool _isFiring = false;
        private float _nextTickTime = 0f;
        private float _currentEnergy;
        private bool _isOverheated = false;
        private HashSet<NetworkObject> _hitBuffer = new HashSet<NetworkObject>();

        public override void Initialize(PlayerTankBrain brain)
        {
            base.Initialize(brain);
            _currentEnergy = maxEnergy;
            useAutoAim = false;
        }

        public override void ProcessInput(bool isShootingHeld)
        {
            bool isBlocked = false;
            if (smartAim != null && muzzlePoint != null) { smartAim.GetAimDirection(transform, muzzlePoint, out isBlocked); }
            if (!isShootingHeld && _isOverheated && _currentEnergy > (maxEnergy * 0.05f)) { _isOverheated = false; }
            if (_currentEnergy <= 0f) { _isOverheated = true; }
            if (isShootingHeld && !isBlocked && !_isOverheated)
            {
                _currentEnergy = Mathf.Max(0f, _currentEnergy - energyDrainRate * Time.deltaTime);
                if (!_isFiring)
                {
                    _isFiring = true;
                    SetStreamStateLocal(true);
                    tankBrain.CmdSetStreamState(true);
                }
            }
            else
            {
                _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy + energyRechargeRate * Time.deltaTime);
                if (_isFiring)
                {
                    _isFiring = false;
                    SetStreamStateLocal(false);
                    tankBrain.CmdSetStreamState(false);
                }
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;
            if (Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + tickRate;
                SendHitsToServer();
            }
        }

        public void RegisterParticleHit(GameObject hitObject)
        {
            if (!isLocalPlayer) return;
            NetworkObject netObj = hitObject.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                _hitBuffer.Add(netObj);
            }
        }

        private void SendHitsToServer()
        {
            if (_hitBuffer.Count > 0)
            {
                float tempDelta = (type == StreamType.Firebird) ? temperaturePerTick : -temperaturePerTick;
                NetworkObject[] hitsArray = new NetworkObject[_hitBuffer.Count];
                _hitBuffer.CopyTo(hitsArray);
                tankBrain.CmdSubmitStreamHits(hitsArray, tempDelta, damage);
                _hitBuffer.Clear();
            }
        }

        private void SetStreamStateLocal(bool active)
        {
            if (streamParticles != null)
            {
                if (active) streamParticles.Play(true);
                else streamParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (streamAudio != null)
            {
                if (active && !streamAudio.isPlaying) streamAudio.Play();
                else if (!active) streamAudio.Stop();
            }
        }
        public void SetRemoteStreamState(bool active) { SetStreamStateLocal(active); }
        public override void PerformRemoteVisualShot(Vector3 aimDirection, bool isBlocked) { }
        public override float GetReloadProgress() { return Mathf.Clamp01(_currentEnergy / maxEnergy); }
    }
}