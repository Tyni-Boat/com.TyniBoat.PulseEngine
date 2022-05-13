using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace PulseEngine
{

    /// <summary>
    /// Fait la previsualisation d'une animation.
    /// </summary>
    public class AnimationPreviewer
    {

        #region Attributes >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        #region time control ###############################################

        /// <summary>
        /// The playback time.
        /// </summary>
        private float _totalPlayBackTime;

        /// <summary>
        /// The normalized returned playback time.
        /// </summary>
        private float _normalizedPlayBackTime;

        /// <summary>
        /// The number of loops Done.
        /// </summary>
        private int _animLoops;

        #endregion

        #region Animation ##################################################


        /// <summary>
        /// The playback motion.
        /// </summary>
        private Playable _playBackMotion;

        /// <summary>
        /// The current playable graph.
        /// </summary>
        private PlayableGraph _graph;

        /// <summary>
        /// Active when previwe is playing anim.
        /// </summary>
        private bool _isPlaying;

        /// <summary>
        /// The editor deltaTime
        /// </summary>
        private float _deltaTime;

        /// <summary>
        /// The time at the last frame;
        /// </summary>
        DateTime _lastFrameTime;

        #endregion

        #region Rendering ##################################################


        /// <summary>
        /// the preview renderer.
        /// </summary>
        private PreviewRenderUtility _previewRenderer;


        #endregion

        #region World Transforms ###########################################


        /// <summary>
        /// the target to render.
        /// </summary>
        private GameObject _previewAvatar;

        /// <summary>
        /// the target's accessories to render.
        /// </summary>
        private Dictionary<HumanBodyBones, (GameObject go, Vector3 offset, Vector3 RotOffset, Vector3 scale)> _accesoriesPool = new Dictionary<HumanBodyBones, (GameObject go, Vector3 offset, Vector3 RotOffset, Vector3 scale)>();

        /// <summary>
        /// arrow indicator
        /// </summary>
        private GameObject _directionArrow;

        /// <summary>
        /// root indicator
        /// </summary>
        private GameObject _rootGameObject;

        /// <summary>
        /// The grond plane mesh.
        /// </summary>
        private GameObject _floorPlane;

        /// <summary>
        /// the floor texture.
        /// </summary>
        private Texture2D _floorTexture;

        /// <summary>
        /// the floor material.
        /// </summary>
        private Material _floorMaterial;

        /// <summary>
        /// the preview cam pivot offset.
        /// </summary>
        private Vector3 _pivotOffset;

        #endregion

        #region Navigation #################################################

        /// <summary>
        /// the zooming factor
        /// </summary>
        private float _zoomFactor = 3;

        /// <summary>
        /// the pan angle around target.
        /// </summary>
        private float _panAngle = 50;

        /// <summary>
        /// the tilt angle around target.
        /// </summary>
        private float _tiltAngle = 50;

        /// <summary>
        /// the target's scale.
        /// </summary>
        private float _targetScale = 1;

        /// <summary>
        /// the view tool used right now
        /// </summary>
        private ViewTool _viewTool = ViewTool.None;

        #endregion


        #endregion

        #region public Methods >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        /// <summary>
        /// Render the preview.
        /// </summary>
        /// <param name="_motion"></param>
        /// <param name="_target"></param>
        public float Previsualize(Motion _motion, float aspectRatio = 1.77f, GameObject _target = null, params (GameObject go, HumanBodyBones bone, Vector3 offset, Vector3 rotation, Vector3 scale)[] accessories)
        {
            _deltaTime = (float)((float)((DateTime.Now - _lastFrameTime).Milliseconds) / 1000f);
            _lastFrameTime = DateTime.Now;

            GUILayout.BeginVertical("GroupBox");
            if (!_motion)
            {
                Reset();
                RenderNull();
                GUILayout.EndVertical();
                return 0;
            }
            if (Initialize(_motion, _target, accessories))
            {
                var r = GUILayoutUtility.GetAspectRect(aspectRatio);
                Rect rect2 = r;
                rect2.yMin += 20f;
                rect2.height = Mathf.Max(rect2.height, 64f);
                int controlID = GUIUtility.GetControlID("Preview".GetHashCode(), FocusType.Passive, rect2);
                Event current = Event.current;
                EventType typeForControl = current.GetTypeForControl(controlID);
                if (typeForControl == EventType.Repaint)
                {
                    RenderPreview(r);
                }
                int controlID2 = GUIUtility.GetControlID("Preview".GetHashCode(), FocusType.Passive);
                typeForControl = current.GetTypeForControl(controlID2);
                HandleViewTool(Event.current, typeForControl, 0, r);
                TimeControl();

            }
            else
                RenderNull();
            GUILayout.EndVertical();

            return _animLoops;
        }

        #endregion

        #region private Methods >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        /// <summary>
        /// Initialise the avatar.
        /// </summary>
        /// <param name="_avatar"></param>
        private bool Initialize(Motion _motion, GameObject _avatar = null, params (GameObject go, HumanBodyBones bone, Vector3 offset, Vector3 rotation, Vector3 scale)[] accessories)
        {
            if (_previewRenderer == null)
            {
                _previewRenderer = new PreviewRenderUtility();
                _pivotOffset = new Vector3(0, 1, 0);
            }
            if (_previewRenderer.camera != null)
            {
                //Camera Transform
                var cx = _zoomFactor * Mathf.Cos(_panAngle * Mathf.Deg2Rad);
                var cy = _zoomFactor * Mathf.Sin(_tiltAngle * Mathf.Deg2Rad);
                var cz = _zoomFactor * Mathf.Sin(_panAngle * Mathf.Deg2Rad);
                if (_previewRenderer.camera)
                {
                    _previewRenderer.camera.transform.localPosition = _pivotOffset + new Vector3(cx, cy, cz);
                    _previewRenderer.camera.transform.LookAt(_pivotOffset, Vector3.up);

                    //Camera config
                    _previewRenderer.camera.fieldOfView = 60;
                    _previewRenderer.camera.nearClipPlane = 0.01f;
                    _previewRenderer.camera.farClipPlane = 100;

                    //Lights and FX
                    SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
                    SetupPreviewLightingAndFx(ambientProbe);
                }

                //Objects on scene
                if (_floorTexture == null)
                {
                    _floorTexture = (Texture2D)EditorGUIUtility.Load("Avatar/Textures/AvatarFloor.png");
                }
                if (_floorMaterial == null)
                {
                    Shader shader = EditorGUIUtility.LoadRequired("Previews/PreviewPlaneWithShadow.shader") as Shader;
                    _floorMaterial = new Material(shader);
                    _floorMaterial.mainTexture = _floorTexture;
                    _floorMaterial.mainTextureScale = Vector2.one * 5f * 4f;
                    _floorMaterial.SetVector("_Alphas", new Vector4(0.5f, 0.3f, 0f, 0f));
                    _floorMaterial.hideFlags = HideFlags.HideAndDontSave;
                    _floorMaterial = new Material(_floorMaterial);
                }
                if (!_floorPlane)
                {
                    _floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    _floorPlane.transform.localScale = Vector3.one * 10;
                    var render = _floorPlane.GetComponent<MeshRenderer>();
                    if (render)
                    {
                        render.material = _floorMaterial;
                    }
                    ResetTransform(_floorPlane);
                    _previewRenderer.AddSingleGO(_floorPlane);
                }
                if (!_directionArrow)
                {
                    var original = (GameObject)EditorGUIUtility.Load("Avatar/dial_flat.prefab");
                    _directionArrow = UnityEngine.Object.Instantiate<GameObject>(original, _previewRenderer.camera.transform);
                    ResetTransform(_directionArrow);
                    _previewRenderer.AddSingleGO(_directionArrow);
                }
                if (!_rootGameObject)
                {
                    var original = (GameObject)EditorGUIUtility.Load("Avatar/root.fbx");
                    _rootGameObject = UnityEngine.Object.Instantiate<GameObject>(original, _previewRenderer.camera.transform);
                    ResetTransform(_rootGameObject);
                    _previewRenderer.AddSingleGO(_rootGameObject);
                }
                if (!_previewAvatar)
                {
                    _previewAvatar = GetAvatar(ref _avatar);
                    _previewAvatar.hideFlags = HideFlags.HideAndDontSave;
                    _previewAvatar.transform.position = Vector3.zero;
                    _previewRenderer.AddSingleGO(_previewAvatar);
                }
                if (!_graph.IsValid())
                {
                    if (_previewAvatar)
                    {
                        var animator = _previewAvatar.GetComponentInChildren<Animator>();
                        if (!animator)
                        {
                            Reset();
                            return false;
                        }

                        InitController(_motion, animator);
                    }
                    else
                    {
                        Reset();
                        return false;
                    }
                }
                for (int i = 0, len = accessories.Length; i < len; i++)
                {
                    var accessory = accessories[i];
                    if (accessory.go == null)
                        continue;
                    if (_accesoriesPool == null)
                        _accesoriesPool = new Dictionary<HumanBodyBones, (GameObject go, Vector3 offset, Vector3 RotOffset, Vector3 scale)>();
                    if (_accesoriesPool.ContainsKey(accessory.bone))
                    {
                        if (_accesoriesPool[accessory.bone].go.name.Contains(accessory.go.name))
                        {
                            _accesoriesPool[accessory.bone] = (_accesoriesPool[accessory.bone].go, accessory.offset, accessory.rotation, accessory.scale);
                            continue;
                        }
                    }
                    var acc = UnityEngine.Object.Instantiate<GameObject>(accessory.go, _previewRenderer.camera.transform);
                    ResetTransform(acc);
                    acc.hideFlags = HideFlags.HideAndDontSave;
                    _accesoriesPool.Add(accessory.bone, (acc, accessory.offset, accessory.rotation, accessory.scale));
                    _previewRenderer.AddSingleGO(acc);
                }
                return true;
            }

            return false;
        }

        #region time control ###########################################

        /// <summary>
        /// the control time interface.
        /// </summary>
        private void TimeControl()
        {
            var clip = ((AnimationClipPlayable)_playBackMotion).GetAnimationClip();
            if (clip == null)
                return;
            //Display controls
            try
            {
                GUILayout.BeginHorizontal("HelpBox");
                if (GUILayout.Button(_isPlaying ? "||" : ">>", new[] { GUILayout.Width(30), GUILayout.Height(20) }))
                {
                    _isPlaying = !_isPlaying;
                }
                if (_isPlaying)
                {
                    var rect2 = GUILayoutUtility.GetRect(50, 20);
                    EditorGUI.ProgressBar(rect2, _normalizedPlayBackTime, "");
                }
                else
                {
                    _totalPlayBackTime = EditorGUILayout.Slider(_totalPlayBackTime, 0, clip.length);
                }
                GUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region Animation ###########################################

        /// <summary>
        /// Initialise the animator controller
        /// </summary>
        private void InitController(Motion motion, Animator anim)
        {
            if (motion == null || anim == null)
            {
                return;
            }

            anim.enabled = true;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.logWarnings = false;
            anim.fireEvents = false;
            //anim.applyRootMotion = false;

            if (!_playBackMotion.IsValid() || (_playBackMotion.IsPlayableOfType<AnimationClipPlayable>() && ((AnimationClipPlayable)_playBackMotion).GetAnimationClip() != motion && motion != null))
            {
                AnimationLayerMixerPlayable mixer;

                //Create playable graph
                if (!_graph.IsValid())
                {
                    _graph = PlayableGraph.Create("PreviweGraph");
                    _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var outPut = AnimationPlayableOutput.Create(_graph, "output", anim);
                    mixer = AnimationLayerMixerPlayable.Create(_graph, 2);
                    Tools.AddAnimationPosePlayable(_graph, mixer, 1);
                    mixer.SetInputWeight(1, 0);
                    outPut.SetSourcePlayable(mixer);
                }
                else
                {
                    mixer = (AnimationLayerMixerPlayable)_graph.GetOutput(0).GetSourcePlayable();
                }

                if (_playBackMotion.IsValid())
                {
                    _playBackMotion.Destroy();
                }
                _playBackMotion = AnimationClipPlayable.Create(_graph, (AnimationClip)motion);
                mixer.ConnectInput(0, _playBackMotion, 0);
                mixer.SetInputWeight(0, 1);
            }
            else if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        /// <summary>
        /// Animate the target
        /// </summary>
        private void Animate()
        {
            if (_graph.IsValid())
            {
                if (_isPlaying)
                {
                    _graph.Evaluate(_deltaTime * 2);
                    _totalPlayBackTime = (float)_playBackMotion.GetTime();
                }
                else
                {
                    _playBackMotion.SetTime(_normalizedPlayBackTime * ((AnimationClipPlayable)_playBackMotion).GetAnimationClip().length);
                    _graph.Evaluate(_totalPlayBackTime);
                }

                if (_playBackMotion.IsValid())
                {
                    var alltime = _totalPlayBackTime / ((AnimationClipPlayable)_playBackMotion).GetAnimationClip().length;
                    _animLoops = (int)alltime;
                    _normalizedPlayBackTime = alltime - _animLoops;
                }
            }
        }

        #endregion

        #region Rendering ###########################################

        /// <summary>
        /// Render the preview.
        /// </summary>
        private void RenderPreview(Rect r)
        {
            if (_previewRenderer == null)
                return;
            //Animation
            Animate();
            //Positionning objects
            PositionPreviewObjects();
            //Positioning floor
            AdjustFloorPosition();
            //Start Rendering
            _previewRenderer.BeginPreview(r, GUIStyle.none);
            bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            _previewRenderer.camera.Render();
            Unsupported.SetRenderSettingsUseFogNoDirty(fog);
            //End rendering.
            Texture texture = _previewRenderer.EndPreview();
            GUI.DrawTexture(r, texture);
        }

        /// <summary>
        /// Render empty preview.
        /// </summary>
        private static void RenderNull()
        {
            var r = GUILayoutUtility.GetAspectRect(16f / 9);
            EditorGUI.DropShadowLabel(new Rect(r.position.x, r.position.y / 2, r.width, r.height), "No Motion to preview.\nPlease select a valid motion to preview.");
        }

        /// <summary>
        /// Set up the lights and FX
        /// </summary>
        /// <param name="probe"></param>
        private void SetupPreviewLightingAndFx(SphericalHarmonicsL2 probe)
        {
            _previewRenderer.lights[0].intensity = 1;
            _previewRenderer.lights[0].color = Color.white;
            _previewRenderer.lights[0].transform.rotation = Quaternion.Euler(45, 0, 0);
            _previewRenderer.lights[1].intensity = 1;
            _previewRenderer.lights[1].transform.rotation = Quaternion.Euler(-135, 0, 0);
            _previewRenderer.lights[1].color = Color.white;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.1f, 1f);
            RenderSettings.ambientProbe = probe;
        }

        #endregion

        #region World Transforms ###########################################

        /// <summary>
        /// Position the gameobjects in scene.
        /// </summary>
        private void PositionPreviewObjects()
        {
            if (_previewAvatar.TryGetComponent<Animator>(out var anim))
            {
                var position = anim.rootPosition;
                position.y = Mathf.Clamp(position.y, 0, position.y);
                var offset = Vector3.Lerp(_pivotOffset, position, _deltaTime * 100);
                _pivotOffset = new Vector3(offset.x, _pivotOffset.y, offset.z);
                _directionArrow.transform.position = anim.rootPosition;
                var rot = Quaternion.Euler(0, anim.transform.eulerAngles.y, 0);
                _directionArrow.transform.rotation = rot;
                _directionArrow.transform.localScale = Vector3.one * _targetScale * 2f;
                _rootGameObject.transform.position = _pivotOffset;
                _rootGameObject.transform.rotation = Quaternion.identity;
                _rootGameObject.transform.localScale = Vector3.one * _targetScale * 0.25f;

                foreach (var acc in _accesoriesPool)
                {
                    var bone = anim.GetBoneTransform(acc.Key);
                    if (acc.Value.go.transform.parent != bone)
                        acc.Value.go.transform.SetParent(bone);
                    acc.Value.go.transform.localPosition = acc.Value.offset;
                    acc.Value.go.transform.localRotation = Quaternion.Euler(acc.Value.RotOffset);
                    acc.Value.go.transform.localScale = acc.Value.scale;
                }
            }
        }

        /// <summary>
        /// Adjust floorMaterial.
        /// </summary>
        private void AdjustFloorPosition()
        {
            if (!_floorPlane)
                return;
            if (_previewAvatar.TryGetComponent<Animator>(out var anim))
            {
                _floorPlane.transform.position = anim.transform.position;
                Vector2 floorTexOffset = _floorMaterial.mainTextureOffset - (new Vector2(anim.velocity.x, anim.velocity.z) / _floorPlane.transform.localScale.x) * _deltaTime * 2;
                _floorMaterial.mainTextureOffset = floorTexOffset;
            }
        }

        /// <summary>
        /// Reset the transform of a GO and void his parent.
        /// </summary>
        /// <param name="go"></param>
        private void ResetTransform(GameObject go)
        {
            if (go == null)
                return;
            go.transform.SetParent(null);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.Euler(Vector3.zero);
        }

        #endregion

        #region Navigation ###########################################

        /// <summary>
        /// Handle the mouse up event
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="id"></param>
        private void HandleMouseUp(Event evt, int id)
        {
            if (GUIUtility.hotControl == id)
            {
                _viewTool = ViewTool.None;
                GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                _viewTool = ViewTool.None;
                evt.Use();
            }
        }

        /// <summary>
        /// Handle the mouse down event.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="id"></param>
        /// <param name="previewRect"></param>
        private void HandleMouseDown(Event evt, int id, Rect previewRect)
        {
            if (_viewTool != 0 && previewRect.Contains(evt.mousePosition))
            {
                EditorGUIUtility.SetWantsMouseJumping(1);
                if (evt.button == 0)
                {
                    _viewTool = ViewTool.Orbit;
                }
                else if (evt.button == 2)
                {
                    _viewTool = ViewTool.Pan;
                }
                evt.Use();
                GUIUtility.hotControl = id;
            }
        }

        /// <summary>
        /// handle the mouse drag event.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="id"></param>
        /// <param name="previewRect"></param>
        private void HandleMouseDrag(Event evt, int id, Rect previewRect)
        {
            if (!(_previewRenderer == null) && GUIUtility.hotControl == id)
            {
                switch (_viewTool)
                {
                    case ViewTool.Orbit:
                        DoAvatarPreviewOrbit(evt, previewRect);
                        break;
                    case ViewTool.Pan:
                        DoAvatarPreviewPan(evt, previewRect);
                        break;
                    case ViewTool.Zoom:
                        DoAvatarPreviewZoom(evt, (0f - HandleUtility.niceMouseDeltaZoom) * ((!evt.shift) ? 0.5f : 2f), previewRect);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Pan the camera.
        /// </summary>
        /// <param name="evt"></param>
        private void DoAvatarPreviewPan(Event evt, Rect previewRect)
        {
            Vector2 camPivot = new Vector2(0, -_pivotOffset.y);
            camPivot -= evt.delta * ((!evt.shift) ? 1 : 3) / Mathf.Min(previewRect.width, previewRect.height) * 12f;
            //camPivot.y = Mathf.Clamp(camPivot.y, 0, 2);
            _pivotOffset.y = -camPivot.y;
            _pivotOffset.y = Mathf.Clamp(_pivotOffset.y, 0, 2);
            evt.Use();
        }

        /// <summary>
        /// Handle the view tool
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="eventType"></param>
        /// <param name="id"></param>
        /// <param name="previewRect"></param>
        protected void HandleViewTool(Event evt, EventType eventType, int id, Rect previewRect)
        {
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.KeyDown:
                case EventType.KeyUp:
                    break;
                case EventType.ScrollWheel:
                    DoAvatarPreviewZoom(evt, HandleUtility.niceMouseDeltaZoom * ((!evt.shift) ? 0.5f : 2f), previewRect);
                    break;
                case EventType.MouseDown:
                    HandleMouseDown(evt, id, previewRect);
                    break;
                case EventType.MouseUp:
                    HandleMouseUp(evt, id);
                    break;
                case EventType.MouseDrag:
                    HandleMouseDrag(evt, id, previewRect);
                    break;
            }
        }

        /// <summary>
        /// Orbit around
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="previewRect"></param>
        private void DoAvatarPreviewOrbit(Event evt, Rect previewRect)
        {
            Vector2 camAxis = new Vector2(_panAngle, -_tiltAngle);
            camAxis -= evt.delta * ((!evt.shift) ? 1 : 3) / Mathf.Min(previewRect.width, previewRect.height) * 140f;
            camAxis.y = Mathf.Clamp(camAxis.y, -90f, 90f);
            _panAngle = camAxis.x;
            _tiltAngle = -camAxis.y;
            evt.Use();
        }

        /// <summary>
        /// zoom
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="delta"></param>
        private void DoAvatarPreviewZoom(Event evt, float delta, Rect previewRect)
        {
            if (previewRect.Contains(evt.mousePosition))
            {
                float num = (0f - delta) * 0.05f;
                _zoomFactor += _zoomFactor * num;
                _zoomFactor = Mathf.Max(_zoomFactor, _targetScale / 10f);
                evt.Use();
            }
        }

        #endregion

        #region Misc ###########################################

        /// <summary>
        /// get the avatar.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private GameObject GetAvatar(ref GameObject original)
        {
            GameObject o = original ? original : (GameObject)EditorGUIUtility.Load("avatar/defaultavatar.fbx");
            var copy = UnityEngine.Object.Instantiate<GameObject>(o, _previewRenderer.camera.transform);
            ResetTransform(copy);
            return copy;
        }

        /// <summary>
        /// Reset the preview.
        /// </summary>
        private void Reset()
        {
            if (_previewRenderer != null)
            {
                _previewRenderer.Cleanup();
            }
            if (_playBackMotion.IsValid())
                _playBackMotion.Destroy();
            _totalPlayBackTime = 0;
        }

        /// <summary>
        /// Close the rendrepreview
        /// </summary>
        public void Destroy()
        {
            Reset();
        }

        public static implicit operator bool(AnimationPreviewer v)
        {
            return v != null;
        }

        ~AnimationPreviewer()
        {
            Reset();
        }

        #endregion

        #endregion

    }
}