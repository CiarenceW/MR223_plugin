using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Receiver2;
using Receiver2ModdingKit;
using UnityEngine;
using RewiredConsts;
using HarmonyLib;

namespace MR223_plugin
{
    public class MR223_Script : ModGunScript
    {
        private float hammer_accel = -5000;
        private float m_charging_handle_amount;
        private ModHelpEntry help_entry;
        public Sprite help_entry_sprite;
        public Transform dust_cover_component;
        private Vector3 magazine_nearly_empty_rot = new Vector3(-24f, -5f, 0f);
        private Vector3 magazine_nearly_empty_pos = new Vector3(-0.1f, 0f, 0.2f);
        private Vector3 magazine_rot = new Vector3(-25f, -60f, 0f);
        private Vector3 magazine_pos = new Vector3(-0.1f, 0f, 0.15f);
        private RotateMover dust_cover = new RotateMover();
        private bool dust_cover_opened;
        private bool slide_prepare_to_lock;
        private bool separator_needs_reset;
        private static bool receiver_broke_open;
        private readonly float[] slide_push_hammer_curve = new float[] {
            0f,
            0f,
            0.35f,
            1f
        };
        LocalAimHandler lah = LocalAimHandler.player_instance;
        public override ModHelpEntry GetGunHelpEntry()
        {
            return help_entry = new ModHelpEntry("MR223")
            {
                info_sprite = help_entry_sprite,
                title = "Heckler & Koch MR223",
                description = "Heckler & Koch Match Rifle .223 Remington\n"
                            + "Capacity: 10 + 1, 223 Remington\n"
                            + "\n"
                            + "Based on the AR-15 platform, the HK 416 improves on it thanks to its proprietary short-stroke gas piston, derived from the G36, itself derived from the AR-18. Thanks to this improvement, the H&K 416 outperformed the M4 in numerous tests conducted by the US Army's Delta Force.\n"
                            + "\n"
                            + "In 2007, H&K introduced the MR223, the civilian variant of the 416, to the European market. This variant would later come to the US under the name of MR556.\n"
                            + "\n"
                            + "In order to be compliant in states with stricter gun laws, civilians need to install special after-market parts, such as pin that block the magazine from being removed until the receiver is opened, or a slide lock that locks open on every shot. Fortunately for them, loopholes that permit the rifle to function somewhat normally exist."
            };
        }
        public override LocaleTactics GetGunTactics()
        {
            return new LocaleTactics()
            {
                gun_internal_name = InternalName,
                title = "Heckler & Koch MR223\n",
                text = "A modded semi-auto rifle, made on a cheese-based diet\n" +
                       "A .223 Remington semi-auto rifle made for the European sporting market, this gun functions mechanically the same as the H&K 416, without the auto fire mode.\n" +
                       "\n" +
                       "This version of the gun is fitted with an auto-locking slide lock in an attempt to be California compliant.\n"+
                       "To safely holster the MR223, flip on the safety."
            };
        }
        /*[HarmonyPatch(typeof(GunScript), "RemoveMag")]
        [HarmonyPrefix]
        private static void patchRemoveMag(ref GunScript __instance)
        {
            MagazineScript magazine = __instance.magazine_instance_in_gun;
            if (!receiver_broke_open)
            {
                return;
            }
        }*/
        public override void InitializeGun()
        {
            pooled_muzzle_flash = ((GunScript)ReceiverCoreScript.Instance().generic_prefabs.First(it => { return it is GunScript && ((GunScript)it).gun_model == GunModel.Deagle; })).pooled_muzzle_flash;
            //loaded_cartridge_prefab = ((GunScript)ReceiverCoreScript.Instance().generic_prefabs.First(it => { return it is GunScript && ((GunScript)it).gun_model == GunModel.Glock; })).loaded_cartridge_prefab;
        }
        public override void AwakeGun()
        {
            hammer.amount = 1;

            dust_cover.transform = dust_cover_component;

            dust_cover.rotations[0] = transform.Find("dust_cover_closed").localRotation;
            dust_cover.rotations[1] = transform.Find("dust_cover_opened").localRotation;
        }
        public override void UpdateGun()
        {
            LocalAimHandler localAimHandler;
            if (LocalAimHandler.TryGetInstance(out localAimHandler)) 
            {
                LocalAimHandler.Hand hand = localAimHandler.hands[0];
                if (hand.slot.contents.Any() && hand.slot.contents[0].type == InventoryItem.Type.Magazine) //checks if there's something in the character's left hand, and if so, if it's a mag.
                {
                    MagazineScript mag = (MagazineScript)hand.slot.contents[0];
                    if (mag.rounds_in_mag <= 3)
                    {
                        mag.hold_offset = magazine_nearly_empty_pos;
                        mag.hold_rotation = magazine_nearly_empty_rot;
                    }
                    else
                    {
                        mag.hold_offset = magazine_pos;
                        mag.hold_rotation = magazine_rot;
                    }
                }
            }
            if (magazine_instance_in_gun != null)
            {
                if (magazine_instance_in_gun.rounds_in_mag <= 3) //checks if the mag in the gun has 3 or less rounds, and if so, updates its position and rotation.
                {
                    magazine_instance_in_gun.hold_offset = magazine_nearly_empty_pos;
                    magazine_instance_in_gun.hold_rotation = magazine_nearly_empty_rot;
                }
                else
                {
                    magazine_instance_in_gun.hold_offset = magazine_pos;
                    magazine_instance_in_gun.hold_rotation = magazine_rot;
                }
            }

            hammer.asleep = true;
            hammer.accel = hammer_accel;

            if((player_input.GetButton(Action.Pull_Back_Slide) && player_input.GetButton(Action.Slide_Lock)) && slide.amount >= slide_lock_position) slide_prepare_to_lock = true; //slide stop held mechanic preventer :(

            if (!slide_prepare_to_lock && player_input.GetButton(Action.Slide_Lock)) //slide stop held mechanic
            {
                _slide_stop_locked = false;
                slide_stop.target_amount = 0f;
                StopSlideStop();
            }
            else if (!player_input.GetButton(Action.Slide_Lock) && slide.vel < 0f && slide.amount > slide_lock_position) //slide force lock mechanic
            {
                _slide_stop_locked = true;
                slide_stop.target_amount = 1f;
                slide_stop.UpdateDisplay();
                StartSlideStop();
            }

            /*if (!receiver_broke_open)
            {
                magazine_catch.amount = 0f;
                magazine_catch.vel = 0f;
                magazine_catch.asleep = true;
                magazine_catch.UpdateDisplay();

                magazine.target_amount = 1f;
                magazine.amount = 1f;
                magazine.vel = 0f;
                magazine.asleep= true;
                magazine.UpdateDisplay();
            }

            ready_to_remove_mag = receiver_broke_open; */

            /*if (Input.GetButtonDown("n"))
            {
                Debug.Log(hand.slot.contents[0].type);
            }*/

            if (slide.amount > 0 && _hammer_state != 3)
            { // Bolt cocks the hammer when moving back 
                hammer.amount = Mathf.Max(hammer.amount, InterpCurve(slide_push_hammer_curve, slide.amount));
            }

            if (hammer.amount == 1) _hammer_state = 3;

            if (IsSafetyOn())
            {
                trigger.amount = Mathf.Min(trigger.amount, 0.1f);

                trigger.UpdateDisplay();
            }

            if (hammer.amount == 0 && _hammer_state == 2)
            { // If hammer dropped and hammer was cocked then fire gun and decock hammer
                TryFireBullet(1, FireBullet);

                _hammer_state = 0;

                _disconnector_needs_reset = true;
            }

            if (trigger.amount == 0)
            {
                _disconnector_needs_reset = false;
            }

            if (slide_stop.amount == 1)
            {
                slide_stop.asleep = true;
            }

            if (slide.amount == 0 && _hammer_state == 3 && _disconnector_needs_reset == false)
            {
                hammer.amount = Mathf.MoveTowards(hammer.amount, _hammer_cocked_val, Time.deltaTime * Time.timeScale * 50);
                if (hammer.amount == _hammer_cocked_val) _hammer_state = 2;
            }

            if (_hammer_state != 3 && ((trigger.amount == 1 && !_disconnector_needs_reset && slide.amount == 0) || hammer.amount != _hammer_cocked_val))
            {
                hammer.asleep = false;
            }

            hammer.TimeStep(Time.deltaTime);

            if (player_input.GetButton(Action.Pull_Back_Slide) || player_input.GetButtonUp(Action.Pull_Back_Slide))
            {
                m_charging_handle_amount = slide.amount;
            }
            else
            {
                m_charging_handle_amount = Mathf.Min(m_charging_handle_amount, slide.amount);
            }

            if ((lah.character_input.GetButtonDown(14) && lah.IsHoldingGun) || (!dust_cover_opened && slide.amount > 0.05f)) //dust cover opening/closing logic
            {
                ToggleDustCover();
            }
            /*if (ca_pin_pusher.amount == 1f)
            {
                ca_pin.asleep = false;
                ca_pin.target_amount = ca_pin_pusher.target_amount;
                ca_pin.accel = 10;
                ca_pin.vel = 10;
            }
            else if (ca_pin_pusher.amount == 0f)
            {
                ca_pin.asleep = false;
                ca_pin.target_amount = ca_pin_pusher.target_amount;
                ca_pin.accel = -1;
                ca_pin.vel = -10;
            }

            separator_needs_reset = (upper_receiver.amount >= 0.1f);

            if (ca_pin.amount == 1f && !separator_needs_reset)
            {
                upper_receiver.asleep = false;
                upper_receiver.target_amount = 1f;
                upper_receiver.accel = 50;
                upper_receiver.vel = 50;
            }
            else if (ca_pin.amount == 0f)
            {
                upper_receiver.asleep = false;
                upper_receiver.target_amount = 0f;
                upper_receiver.accel = -1;
                upper_receiver.vel = -10;
            }

            receiver_broke_open = !(upper_receiver.amount == 0f);*/


            ApplyTransform("charging_handle", m_charging_handle_amount, transform.Find("upper_receiver/charging_handle"));
            ApplyTransform("charging_handle_latch", m_charging_handle_amount, transform.Find("upper_receiver/charging_handle/charging_handle_latch"));

            /*upper_receiver.UpdateDisplay();
            upper_receiver.TimeStep(Time.deltaTime);

            ca_pin_pusher.UpdateDisplay();
            ca_pin_pusher.TimeStep(Time.deltaTime);

            ca_pin.UpdateDisplay();
            ca_pin.TimeStep(Time.deltaTime);*/

            dust_cover.UpdateDisplay();
            dust_cover.TimeStep(Time.deltaTime);

            hammer.UpdateDisplay();

            slide_stop.UpdateDisplay();

            UpdateAnimatedComponents();
        }
        private void ToggleDustCover()
        {
            dust_cover.asleep = false;
            if (dust_cover.target_amount == 1f && slide.amount <= 0.03f)
            {
                dust_cover.target_amount = 0f;
                dust_cover.accel = -1f;
                dust_cover.vel = -10f;
                AudioManager.PlayOneShotAttached(sound_safety_off, dust_cover.transform.gameObject);
                dust_cover_opened = false;
            }
            else if (dust_cover_opened == false)
            {
                dust_cover.target_amount = 1f;
                dust_cover.accel = 1;
                dust_cover.vel = 10;
                AudioManager.PlayOneShotAttached(sound_safety_on, dust_cover.transform.gameObject);
                dust_cover_opened = true;
            }

        }
    }
}
