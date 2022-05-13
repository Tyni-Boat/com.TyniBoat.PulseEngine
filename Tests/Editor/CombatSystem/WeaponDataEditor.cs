using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;



namespace PulseEngine.CombatSystem
{

    /// <summary>
    /// The editor of weapon datas
    /// </summary>
    [InitializeOnLoad]
    public class WeaponDataEditor : PulseEditor<WeaponDatas>
    {

        /// <Summary>
        /// Declare here every attribute used for visual behaviour of the editor window.
        /// </Summary>
        #region Visual Attributes ################################################################################################################################################################################################

        private int _selectedAnimationOverride = -1;

        private bool _hitFramesFolded = false;
        private bool _chainPhasesFolded = false;
        private bool _previewWeaponRestPlace;

        private GameObject _weaponPreview;

        #endregion

        /// <Summary>
        /// Declare here every attribute used for deep behaviour ot the editor window.
        /// </Summary>
        #region Fonctionnal Attributes ################################################################################################################################################################################################

        protected override string Save_Path => "CombatSystem/WeaponDatas";

        protected override string SaveFileName => "WeaponData";


        #endregion

        /// <Summary>
        /// Implement here Methods To Open the window, and register to OnCacheRefresh
        /// </Summary>
        #region Door Methods ################################################################################################################################################################################################


        [MenuItem(PulseConstants.Menu_EDITOR_MENU + "/Combat System/Weapon Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<WeaponDataEditor>();
            window.Show();
        }

        #endregion

        /// <Summary>
        /// Implement here Methods related to GUI.
        /// </Summary>
        #region GUI Methods ################################################################################################################################################################################################

        #endregion

        /// <Summary>
        /// Implement here behaviours methods.
        /// </Summary>
        #region Fontionnal Methods ################################################################################################################################################################################################


        private void Initialisation()
        {
            dataList.Clear();
            for (int i = 0; i < 100; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<WeaponDatas>(PulseConstants.GAME_RES_PATH + Save_Path + "/" + SaveFileName + "_" + i + ".asset");
                if (asset != null)
                {
                    dataList.Add(asset);
                }
            }
        }

        private void OverrideControllerList(WeaponDatas data)
        {
            if (data == null)
                return;
            GroupGUI(() =>
            {
                var ovController = EditorGUILayout.ObjectField("Override Controller", data.AnimatorOverrideController, typeof(AnimatorOverrideController), false) as AnimatorOverrideController;
                data.WriteField("_animatorOverrideController", ovController);
                if (data.AnimatorOverrideController == null)
                    return;
                data.AnimatorOverrideController.runtimeAnimatorController = EditorGUILayout.ObjectField("Controller Based On", data.AnimatorOverrideController.runtimeAnimatorController
                    , typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;
                if (data.AnimatorOverrideController.runtimeAnimatorController == null)
                {
                    EditorGUILayout.LabelField("The Override controller is not based on any animator controller");
                    return;
                }
                _selectedAnimationOverride = MakeList(_selectedAnimationOverride, data.AnimatorOverrideController.runtimeAnimatorController.animationClips.CollectionOf(an => an.name));
            }, "Attacks Params");
        }

        private void GlobalParameters(WeaponDatas data)
        {
            if (data == null)
                return;
            EditorGUILayout.BeginHorizontal();
            GroupGUI(() =>
            {
                var bone = (HumanBodyBones)(EditorGUILayout.EnumPopup("Rest Bone", data.RestBone));
                data.WriteField("_restBone", bone);
                var pos = EditorGUILayout.Vector3Field("Position Offset", data.RestParams.position);
                var rot = EditorGUILayout.Vector3Field("Rotation Offset", data.RestParams.rotation);
                var sca = EditorGUILayout.Vector3Field("Scale Offset", data.RestParams.scale);
                data.WriteField("_restParams", new TransformParams { position = pos, rotation = rot, scale = sca });
            }, "Rest Params", 160);
            GroupGUI(() =>
            {
                var bone = (HumanBodyBones)(EditorGUILayout.EnumPopup("Equip Bone", data.EquipBone));
                data.WriteField("_equipBone", bone);
                var pos = EditorGUILayout.Vector3Field("Position Offset", data.EquipParams.position);
                var rot = EditorGUILayout.Vector3Field("Rotation Offset", data.EquipParams.rotation);
                var sca = EditorGUILayout.Vector3Field("Scale Offset", data.EquipParams.scale);
                data.WriteField("_equipParams", new TransformParams { position = pos, rotation = rot, scale = sca });
            }, "Equip Params", 160);
            EditorGUILayout.EndHorizontal();
        }

        private void BasicInfos(WeaponDatas data)
        {
            if (data == null)
                return;
            GroupGUI(() =>
            {
                EditorGUILayout.TextField("ID", data.Id.ToString());
                string name = EditorGUILayout.TextField("Name", data.Name);
                data.WriteField("_name", name);
                string desc = EditorGUILayout.TextField("Description", data.Description);
                data.WriteField("_description", desc);
            }, "Basic Infos", 120);
        }

        private void OverrideParams(WeaponDatas data)
        {
            if (data == null)
                return;
            string ov_baseName = "";
            string ov_OverideName = "";
            AnimatorOverrideController animatorOverrideController = null;
            if (data.AnimatorOverrideController != null)
            {
                animatorOverrideController = data.AnimatorOverrideController;
                if (animatorOverrideController != null)
                {
                    if (animatorOverrideController.runtimeAnimatorController.animationClips.IsInRange(_selectedAnimationOverride))
                    {
                        ov_baseName = animatorOverrideController.runtimeAnimatorController.animationClips[_selectedAnimationOverride]?.name;
                        ov_OverideName = animatorOverrideController.animationClips[_selectedAnimationOverride]?.name;
                    }
                }
            }
            if (animatorOverrideController == null)
                return;
            if (string.IsNullOrEmpty(ov_baseName) || string.IsNullOrEmpty(ov_OverideName))
                return;
            GroupGUI(() =>
            {
                string color = ov_OverideName == ov_baseName ? "green" : "yellow";
                string symbol = ov_OverideName == ov_baseName ? "=" : ">>";
                EditorGUILayout.LabelField("Override", $"<color={color}>{ov_baseName}</color> <color=white>{symbol}</color> <color={color}>{ov_OverideName}</color>", new GUIStyle { richText = true });
                var Ov_anim = (AnimationClip)EditorGUILayout.ObjectField("Override Animation", ov_OverideName == ov_baseName ? null : animatorOverrideController[ov_baseName], typeof(AnimationClip), false);
                animatorOverrideController[ov_baseName] = Ov_anim;
                var prevWeapon = (GameObject)EditorGUILayout.ObjectField("Weapon Preview", _weaponPreview, typeof(GameObject), false);
                if (prevWeapon != _weaponPreview)
                {
                    _weaponPreview = prevWeapon;
                    ResetPreviews();
                }
                PreviewAnAnimation(Ov_anim, 32 / 9, null, _previewWeaponRestPlace
                    ? (_weaponPreview, data.RestBone, data.RestParams.position, data.RestParams.rotation, data.RestParams.scale)
                    : (_weaponPreview, data.EquipBone, data.EquipParams.position, data.EquipParams.rotation, data.EquipParams.scale));
                EditorGUILayout.BeginHorizontal();
                int pose = GUILayout.Toolbar(_previewWeaponRestPlace ? 1 : 0, new[] { "Equip Pose", "Rest Pose" });
                bool poseBool = pose > 0;
                if (poseBool != _previewWeaponRestPlace)
                {
                    _previewWeaponRestPlace = poseBool;
                    ResetPreviews();
                }
                EditorGUILayout.EndHorizontal();
            }, "Override Parameters");

            GroupGUI(() =>
            {
                if (data.Attacks == null)
                    data.WriteField("_attacks", new List<AttackParams>());
                if (data.Attacks.TryGetValue(ov_baseName, out var atkValue))
                {
                    if (GUILayout.Button("Delete Attack"))
                    {
                        data.Attacks.Remove(ov_baseName);
                    }
                    EditorGUILayout.Space();

                //Physic space
                EditorGUILayout.BeginVertical("GroupBox");
                    var space = (PhysicSpace)EditorGUILayout.EnumPopup("Physic Space", atkValue.PhysicSpace);
                    atkValue.WriteField("_physicSpace", space);
                    EditorGUILayout.EndVertical();


                //hit frames
                if (atkValue.HitFrames == null)
                       atkValue.WriteField("_hitFrames", new List<HItFrame>());
                    if (GUILayout.Button("Add Hit Frame"))
                    {
                        atkValue.HitFrames.Add(new HItFrame() { BoxSize = Vector3.one });
                    }
                    _hitFramesFolded = EditorGUILayout.BeginFoldoutHeaderGroup(_hitFramesFolded, "Hit Frames");
                    {
                        for (int i = 0; i < atkValue.HitFrames.Count; i++)
                        {
                            int k = i;
                            GroupGUInoStyle(() =>
                            {
                                float startTime = atkValue.HitFrames[k].ImpactTime;
                                float endTime = atkValue.HitFrames[k].EndTime;
                                EditorGUILayout.MinMaxSlider($"Normal Time ({startTime.ToString("f2")}-{endTime.ToString("f2")})", ref startTime, ref endTime, 0, 1);
                                atkValue.HitFrames[k].ImpactTime = startTime;
                                atkValue.HitFrames[k].EndTime = endTime;
                                int hitCnt = EditorGUILayout.IntField("Hit Every X frame", atkValue.HitFrames[k].HitEveryXFrames);
                                atkValue.HitFrames[k].HitEveryXFrames = Mathf.Clamp(hitCnt, 0, int.MaxValue);
                                atkValue.HitFrames[k].ImpactDamagesmultiplier = EditorGUILayout.FloatField("Damages Multiplier", atkValue.HitFrames[k].ImpactDamagesmultiplier);
                                atkValue.HitFrames[k].ImpactDirection = (Direction)EditorGUILayout.EnumPopup("Impact Direction", atkValue.HitFrames[k].ImpactDirection);
                                atkValue.HitFrames[k].ImpactIntensity = (HitIntensity)EditorGUILayout.EnumPopup("Impact Intensity", atkValue.HitFrames[k].ImpactIntensity);
                                atkValue.HitFrames[k].UseWeaponCollider = EditorGUILayout.Toggle("Use Weapon Collider", atkValue.HitFrames[k].UseWeaponCollider);
                                if (!atkValue.HitFrames[k].UseWeaponCollider)
                                {
                                    atkValue.HitFrames[k].SourceBone = (HumanBodyBones)EditorGUILayout.EnumPopup("Hit box Bone", atkValue.HitFrames[k].SourceBone);
                                    atkValue.HitFrames[k].BoxOffset = EditorGUILayout.Vector3Field("HitBox position Offset", atkValue.HitFrames[k].BoxOffset);
                                    atkValue.HitFrames[k].BoxSize = EditorGUILayout.Vector3Field("HitBox Scale", atkValue.HitFrames[k].BoxSize);
                                }
                                if (GUILayout.Button("Delete"))
                                {
                                    atkValue.HitFrames.RemoveAt(k);
                                }
                            }, $"Hit {i}", 50);
                        }
                    }
                    atkValue.WriteField("_hitFrames", atkValue.HitFrames);
                    EditorGUILayout.EndFoldoutHeaderGroup();


                //Chain phases
                if (atkValue.ChainsPhases == null)
                        atkValue.WriteField("_chainsPhases", new List<ComboChainPhase>());
                    if (GUILayout.Button("Add Combo Phase"))
                    {
                        atkValue.ChainsPhases.Add(new ComboChainPhase());
                    }
                    _chainPhasesFolded = EditorGUILayout.BeginFoldoutHeaderGroup(_chainPhasesFolded, "Combo Chain Phases");
                    {
                        for (int i = 0; i < atkValue.ChainsPhases.Count; i++)
                        {
                            int k = i;
                            GroupGUInoStyle(() =>
                            {
                                float startTime = atkValue.ChainsPhases[k].TriggerTime;
                                float endTime = atkValue.ChainsPhases[k].EndTime;
                                EditorGUILayout.MinMaxSlider($"Normal Time ({startTime.ToString("f2")}-{endTime.ToString("f2")})", ref startTime, ref endTime, 0, 1);
                                atkValue.ChainsPhases[k].TriggerTime = startTime;
                                atkValue.ChainsPhases[k].EndTime = endTime;
                                atkValue.ChainsPhases[k].NextStateName = EditorGUILayout.TextField("Next State Name", atkValue.ChainsPhases[k].NextStateName);
                                atkValue.ChainsPhases[k].NextStateTransition = EditorGUILayout.Slider("Normalized Transition", atkValue.ChainsPhases[k].NextStateTransition, 0, 0.5f);
                                if (GUILayout.Button("Delete"))
                                {
                                    atkValue.ChainsPhases.RemoveAt(k);
                                }
                            }, $"Chain Phase {i}", 50);
                        }
                    }
                   atkValue.WriteField("_chainsPhases", atkValue.ChainsPhases);
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                else
                {
                    if (GUILayout.Button("Create Attack"))
                    {
                        var atk = new AttackParams();
                        atk.WriteField("_overrideAnimName", ov_baseName);
                        data.Attacks.Add(ov_baseName, atk);
                    }
                }
            }, "Attack Parameters");
        }

        #endregion

        /// <Summary>
        /// Implement here overrides methods.
        /// </Summary>
        #region Program FLow Methods ################################################################################################################################################################################################

        /// <summary>
        /// refraichie.
        /// </summary>
        protected override void OnRedraw()
        {
            base.OnRedraw();
        }

        /// <summary>
        /// initialise.
        /// </summary>
        protected override void OnInitialize()
        {
            Initialisation();
        }

        /// <summary>
        /// a la fermeture.
        /// </summary>
        protected override void OnQuit()
        {
            //if (animPreview != null)
            //    animPreview.Destroy();
            //try
            //{
            //    OnCacheRefresh -= RefreshCache;
            //}
            //catch { }
        }

        protected override void OnListChange()
        {
            ResetPreviews();
            _selectedAnimationOverride = -1;
        }

        protected override void OnHeaderChange()
        {
            //if (animPreview != null)
            //    animPreview.Destroy();
            //animPreview = null;
            //animPreview = new Previewer();
        }

        protected override void OnBodyRedraw()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
            BasicInfos(data);
            GlobalParameters(data);
            OverrideControllerList(data);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            OverrideParams(data);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        protected override void OnHeaderRedraw()
        {

        }

        #endregion

        /// <Summary>
        /// Implement here miscelaneous methods relative to the module in editor mode.
        /// </Summary>
        #region Helpers & Tools ################################################################################################################################################################################################

        #endregion

    }

}