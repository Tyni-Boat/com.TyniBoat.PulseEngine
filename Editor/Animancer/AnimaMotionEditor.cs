using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;


namespace PulseEngine.Animancer
{

    /// <summary>
    /// Custom editor of Anima Motions
    /// </summary>
    public class AnimaMotionEditor : PulseEditor<AnimaMotion>
    {

        /// <Summary>
        /// Declare here every attribute used for visual behaviour of the editor window.
        /// </Summary>
        #region Visual Attributes ################################################################################################################################################################################################


        #endregion

        /// <Summary>
        /// Declare here every attribute used for deep behaviour ot the editor window.
        /// </Summary>
        #region Fonctionnal Attributes ################################################################################################################################################################################################

        protected override string Save_Path => "Anima/Motions";

        protected override string SaveFileName => "AniMotion";


        #endregion

        /// <Summary>
        /// Implement here Methods To Open the window, and register to OnCacheRefresh
        /// </Summary>
        #region Door Methods ################################################################################################################################################################################################


        [MenuItem(PulseConstants.Menu_EDITOR_MENU + "/Anima/Motion Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<AnimaMotionEditor>();
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
                var asset = AssetDatabase.LoadAssetAtPath<AnimaMotion>(PulseConstants.GAME_RES_PATH + Save_Path + "/" + SaveFileName + "_" + i + ".asset");
                if (asset != null)
                {
                    dataList.Add(asset);
                }
            }
        }

        private void GlobalParameters(AnimaMotion data)
        {
            if (data == null)
                return;
            GroupGUI(() =>
            {
                if (data.Clips == null)
                    data.Clips = new AnimationClip[0];
                int clipsNumber = EditorGUILayout.IntField("Clip Number", data.Clips.Length);
                if (clipsNumber != data.Clips.Length)
                {
                    AnimationClip[] lastClips = new AnimationClip[data.Clips.Length];
                    data.Clips.CopyTo(lastClips, 0);
                    data.Clips = new AnimationClip[clipsNumber];
                    for (int i = 0; i < data.Clips.Length; i++)
                    {
                        if (lastClips.IsInRange(i))
                            data.Clips[i] = lastClips[i];
                    }
                }
                for (int i = 0; i < data.Clips.Length; i++)
                {
                    var clip = EditorGUILayout.ObjectField($"Animation {i + 1}", data.Clips[i], typeof(AnimationClip), true) as AnimationClip;
                    if (clip != data.Clips[i])
                    {
                        data.BlendController = null;
                        data.Clips[i] = clip;
                    }
                }
                data.Priority =  EditorGUILayout.IntField("Priority", data.Priority);
                data.UseRootMotion =  EditorGUILayout.Toggle("Use Root Motion", data.UseRootMotion);

                if(GUILayout.Button("Generate RuntimeController"))
                {
                    ConstructRuntimeController(data);
                    AssetDatabase.SaveAssets();
                }
            }, "Animation Params", 150);
        }

        private void BasicInfos(AnimaMotion data)
        {
            if (data == null)
                return;
            GroupGUI(() =>
            {
                EditorGUILayout.TextField("ID", data.Id.ToString());
                string name = EditorGUILayout.TextField("Name", data.Name);
                data.Statename = name;
                data.WriteField("_name", name);
                string desc = EditorGUILayout.TextField("Description", data.Description);
                data.WriteField("_description", desc);
            }, "Basic Infos", 150);
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

        }

        protected override void OnListChange()
        {
            ResetPreviews();
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
            //Column 1
            EditorGUILayout.BeginVertical();
            BasicInfos(data);
            GlobalParameters(data);
            EditorGUILayout.EndVertical();
            //Column 2
            EditorGUILayout.BeginVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        protected override void OnHeaderRedraw()
        {

        }

        protected override void OnRemoveAssetDelete(AnimaMotion asset)
        {
            base.OnRemoveAssetDelete(asset);
            if (asset == null)
                return;
            if (asset.BlendController != null)
            {
                string path = AssetDatabase.GetAssetPath(asset.BlendController);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        #endregion

        /// <Summary>
        /// Implement here miscelaneous methods relative to the module in editor mode.
        /// </Summary>
        #region Helpers & Tools ################################################################################################################################################################################################

        public void ConstructRuntimeController(AnimaMotion motion)
        {
            if (motion == null)
                return;
            string assetName = SaveFileName + $"BlendController_{motion.Id}";
            string assetPath = Path.Combine(PulseConstants.GAME_RES_PATH + Save_Path, $"{assetName}.controller");
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                //return;
            }

            // Creates the controller
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            //var controller = new UnityEditor.Animations.AnimatorController();
            controller.name = assetName;
            motion.BlendController = controller;

            //Add Layer
            controller.AddLayer("Base");

            // Add parameters
            controller.AddParameter("X", AnimatorControllerParameterType.Float);
            controller.AddParameter("Y", AnimatorControllerParameterType.Float);

            // StateMachine
            var rootStateMachine = controller.layers[0].stateMachine;

            // Add States
            var state = rootStateMachine.AddState("state");
            state.motion = new BlendTree();
            BlendTree tree = state.motion as BlendTree;
            if (tree != null)
            {
                tree.name = motion.Name;
                for (int i = 0; i < motion.Clips.Length; i++)
                {
                    tree.AddChild(motion.Clips[i]);
                }
                tree.useAutomaticThresholds = true;
                tree.blendType = motion.BlendControllerDualParameters ? BlendTreeType.SimpleDirectional2D : BlendTreeType.Simple1D;
                tree.blendParameter = "Y";
                tree.blendParameterY = "X";
            }
            //EditorUtility.SetDirty(controller);
            //AssetDatabase.AddObjectToAsset(controller, motion);
            AssetDatabase.SaveAssets();
            //motion.serializedController = EditorJsonUtility.ToJson(controller);
        }

        #endregion
    }

}