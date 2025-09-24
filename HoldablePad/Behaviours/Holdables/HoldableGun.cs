﻿using HoldablePad.Utils;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace HoldablePad.Behaviours.Holdables
{
    public class HoldableGun : MonoBehaviour
    {
        public Gun ReferenceGun;

        public Holdable ReferenceHoldable;
        public bool IsLeft;

        private float LastTime;

        // On default the object is disabled so Start won't do any good
        public void Initalize()
        {
            ReferenceGun = new Gun
            {
                ProjectileObject = transform.Find("UsedBulletGameObject"),
                ProjectileSource = transform.Find("UsedBulletSoundEffect").GetComponent<AudioSource>()
            };
            if (ReferenceGun.ProjectileObject == null || ReferenceGun.ProjectileSource == null) return;

            // Recycled from base Plugin, still works fine
            string handheldData = ReferenceGun.ProjectileObject.GetComponent<Text>().text;
            string[] handheldInfo = handheldData.Split('$');

            CultureInfo englishUnitedStates = new CultureInfo("en-US");
            ReferenceGun.ProjectSpeed = float.Parse(handheldInfo[0], englishUnitedStates);
            ReferenceGun.ProjectCooldown = float.Parse(handheldInfo[1], englishUnitedStates);
            ReferenceGun.LoopAudio = bool.Parse(handheldInfo[2]);

            ReferenceGun.ProjectileSource.loop = ReferenceGun.LoopAudio;
            if (ReferenceGun.ProjectileSource.spatialBlend == 0)
                ReferenceGun.ProjectileSource.volume = Mathf.Clamp(ReferenceGun.ProjectileSource.volume, 0.0f, 0.1f);

            if (handheldInfo.ElementAtOrDefault(3) != null && bool.Parse(handheldInfo[3]))
            {
                ReferenceGun.VibrationModule = true;
                ReferenceGun.VibrationAmp = float.Parse(handheldInfo[4], englishUnitedStates);
                ReferenceGun.VibrationDur = float.Parse(handheldInfo[5], englishUnitedStates);
            }

            IsLeft = bool.Parse(ReferenceHoldable.GetHoldableProp(Holdable.HoldablePropType.IsLeftHand).ToString());
            ReferenceGun.ProjectileObject.gameObject.SetActive(false);
        }

        public void Update()
        {
            VRRig ReferenceRig = GorillaTagger.Instance.offlineVRRig;
            bool triggerHeld = IsLeft ? ReferenceRig.leftIndex.calcT >= 0.65f : ReferenceRig.rightIndex.calcT >= 0.65f;

            if (Time.time >= LastTime && triggerHeld)
            {
                LastTime = Time.time + ReferenceGun.ProjectCooldown;
                ProjectProjectile();
            }

            if (ReferenceGun.LoopAudio)
            {
                if (triggerHeld)
                {
                    GorillaTagger.Instance.StartVibration(IsLeft, ReferenceGun.VibrationAmp, Time.deltaTime);
                    if (!ReferenceGun.ProjectileSource.isPlaying)
                        ReferenceGun.ProjectileSource.Play();
                }
                else if (!triggerHeld && ReferenceGun.ProjectileSource.isPlaying)
                    ReferenceGun.ProjectileSource.Stop();
            }
        }

        public void ProjectProjectile() // To whoever is reading this, please let me know on a scale of 1-10 how good I am at naming my methods  //ZlothY - very good well done :3
        {
            if (!ReferenceGun.LoopAudio)
            {
                ReferenceGun.ProjectileSource.Play();
                if (ReferenceGun.VibrationModule)
                    GorillaTagger.Instance.StartVibration(IsLeft, ReferenceGun.VibrationAmp, ReferenceGun.VibrationDur);
            }

            var ProjectedBullet = Instantiate(ReferenceGun.ProjectileObject.gameObject);
            ProjectedBullet.SetActive(true);

            var TempBulletTransform = ProjectedBullet.transform;
            TempBulletTransform.SetParent(null, false);
            TempBulletTransform.transform.position = ReferenceGun.ProjectileObject.position;
            TempBulletTransform.transform.rotation = ReferenceGun.ProjectileObject.rotation;
            TempBulletTransform.transform.localScale = ReferenceGun.ProjectileObject.localScale;

            foreach (var component in TempBulletTransform.GetComponentsInChildren<ParticleSystem>()) component.Play();
            foreach (var component in TempBulletTransform.GetComponentsInChildren<TrailRenderer>())
                component.enabled = true;
            foreach (var component in TempBulletTransform.GetComponentsInChildren<Light>()) component.enabled = true;

            var localProjectile = ProjectedBullet.AddComponent<HoldableProjectile>();
            localProjectile.ReferenceHoldable = this;
            localProjectile.targetVelocity = (PlayerUtils.GetPalm(IsLeft).up - Vector3.up * 0.18f) *
                                             ReferenceGun.ProjectSpeed * Constants.ProjectileMultiplier;
        }
    }
}