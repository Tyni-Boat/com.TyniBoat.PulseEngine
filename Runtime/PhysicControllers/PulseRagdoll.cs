using PulseEngine.CharacterControl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace PulseEngine
{

    /// <summary>
    /// The ragdoll system component
    /// </summary>
    public class PulseRagdoll : MonoBehaviour
    {
        #region References ###################################################################

        private Animator _animator;
        private Rigidbody _body;

        [SerializeField] private bool _debug;

        #endregion
        #region Attributes ###################################################################

        /// <summary>
        /// Represent the list of ragdoll bones
        /// </summary>
        private Dictionary<AvatarTarget, (Collider[] colliders, float inactiveTime)> _ragdollBones = new Dictionary<AvatarTarget, (Collider[] colliders, float inactiveTime)>();

        #endregion
        #region Propertie ###################################################################
        #endregion
        #region Functions ###################################################################

        /// <summary>
        /// Create the ragdoll hierarchie.
        /// </summary>
        public void CreateRagdoll()
        {
            if (!_animator)
                return;
            if (!_animator.isHuman)
                return;

            bool initialKineState = true;
            Vector2 hipsSize = Vector2.zero;

            //Get bones
            var hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            var leftButt = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var leftKnee = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightButt = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var rightKnee = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            var rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);

            var spine1 = _animator.GetBoneTransform(HumanBodyBones.Spine);
            var spine2 = _animator.GetBoneTransform(HumanBodyBones.Chest);
            var spine3 = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
            var neck = _animator.GetBoneTransform(HumanBodyBones.Neck);

            var leftArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftElbow = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightElbow = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);

            var head = _animator.GetBoneTransform(HumanBodyBones.Head);

            //Hips
            {
                if (hips && leftButt && rightButt && spine1)
                {
                    var col = hips.gameObject.AddComponent<CapsuleCollider>();
                    col.direction = 0;
                    col.radius = (leftButt.localPosition - rightButt.localPosition).magnitude * 0.33f;
                    col.height = (leftButt.localPosition - rightButt.localPosition).magnitude + 2 * col.radius;
                    hipsSize.y = col.radius;
                    hipsSize.x = col.height;

                    var col2 = hips.gameObject.AddComponent<CapsuleCollider>();
                    col2.direction = 1;
                    col2.radius = col.radius;
                    col2.height = (spine1.position - hips.position).magnitude;
                    col2.center = hips.InverseTransformPoint((spine1.position - hips.position) * 0.5f + hips.position);
                    //
                    var rig = hips.gameObject.AddComponent<Rigidbody>();
                    rig.isKinematic = initialKineState;
                    rig.mass = _body ? _body.mass * 0.1f : 1;
                    if (!_debug)
                    {
                        col.hideFlags = HideFlags.HideInInspector;
                        col2.hideFlags = HideFlags.HideInInspector;
                        rig.hideFlags = HideFlags.HideInInspector;
                    }

                    _ragdollBones.Add(AvatarTarget.Root, (new[] { col, col2 }, -1));
                }
            }
            //Left leg
            {
                AddLeg(AvatarTarget.LeftFoot, leftButt, leftKnee, leftFoot, initialKineState);
            }
            //Right leg
            {
                AddLeg(AvatarTarget.RightFoot, rightButt, rightKnee, rightFoot, initialKineState);
            }
            //Spine
            {
                AddSpine(AvatarTarget.Body, initialKineState, hipsSize, neck, spine1, spine2, spine3);
            }
            //Left Arm
            {
                AddArm(AvatarTarget.LeftHand, leftArm, leftElbow, leftHand, initialKineState);
            }
            //Right Arm
            {
                AddArm(AvatarTarget.RightHand, rightArm, rightElbow, rightHand, initialKineState);
            }
            //Head
            {
                AddHead(initialKineState, hipsSize, head, neck);
            }

            //Tel the character controller about childrens
            if (gameObject.TryGetComponent<BaseCharacterController>(out var controller))
            {
                controller.RefreshChildrenColliders();
            }
        }

        /// <summary>
        /// Destroy ragdoll hierarchie
        /// </summary>
        public void DestroyRagdoll()
        {
            if (_ragdollBones == null)
                return;
            for (int j = 0; j < _ragdollBones.Count; j++)
            {
                var part = _ragdollBones.ElementAt(j);
                if (part.Value.colliders != null)
                {
                    for (int i = 0; i < part.Value.colliders.Length; i++)
                    {
                        if (!part.Value.colliders[i])
                            continue;
                        if (part.Value.colliders[i].TryGetComponent<CharacterJoint>(out var joint))
                            Destroy(joint);
                        if (part.Value.colliders[i].attachedRigidbody)
                            Destroy(part.Value.colliders[i].attachedRigidbody);
                        Destroy(part.Value.colliders[i]);
                    }
                }
            }
            _ragdollBones.Clear();
        }

        /// <summary>
        /// Switch ragdoll state
        /// </summary>
        /// <param name="kinematicState"></param>
        /// <param name="gravityState"></param>
        private void SwitchRagDollMode(bool kinematicState, bool gravityState, AvatarTarget target = AvatarTarget.Root, float time = -1)
        {
            if (_ragdollBones == null)
                return;
            if (target == AvatarTarget.Root)
            {
                for (int j = 0; j < _ragdollBones.Count; j++)
                {
                    var part = _ragdollBones.ElementAt(j);
                    if (part.Value.colliders != null)
                    {
                        for (int i = 0; i < part.Value.colliders.Length; i++)
                        {
                            if (part.Value.colliders[i].attachedRigidbody)
                            {
                                part.Value.colliders[i].attachedRigidbody.isKinematic = kinematicState;
                                part.Value.colliders[i].attachedRigidbody.useGravity = gravityState;
                            }
                        }
                        _ragdollBones[part.Key] = (_ragdollBones[part.Key].colliders, time);
                    }
                }
                return;
            }
            if (_ragdollBones.ContainsKey(target) && _ragdollBones[target].colliders != null)
            {
                for (int i = 0; i < _ragdollBones[target].colliders.Length; i++)
                {
                    if (_ragdollBones[target].colliders[i].attachedRigidbody)
                    {
                        _ragdollBones[target].colliders[i].attachedRigidbody.isKinematic = kinematicState;
                        _ragdollBones[target].colliders[i].attachedRigidbody.useGravity = gravityState;
                    }
                }
                _ragdollBones[target] = (_ragdollBones[target].colliders, time);
            }
        }


        private void AddHead(bool initialKineState, Vector2 hipsSize, Transform head, Transform neck = null)
        {
            if (head && _ragdollBones.ContainsKey(AvatarTarget.Body))
            {
                var col = head.gameObject.AddComponent<SphereCollider>();
                var headTop = head.GetChild(0);
                if (headTop)
                {
                    col.radius = headTop.localPosition.magnitude * 0.5f;
                    col.center = headTop.localPosition * 0.5f;
                }
                else
                {
                    col.radius = hipsSize.x * 0.5f;
                    col.center = new Vector3(0, col.radius * 0.5f, 0);
                }
                //
                var rig = head.gameObject.AddComponent<Rigidbody>();
                rig.isKinematic = initialKineState;
                rig.mass = _body ? (neck ? _body.mass * 0.045f : _body.mass * 0.05f) : 1;
                //
                var join1 = head.gameObject.AddComponent<CharacterJoint>();
                join1.autoConfigureConnectedAnchor = true;
                join1.connectedBody = _ragdollBones[AvatarTarget.Body].colliders[_ragdollBones[AvatarTarget.Body].colliders.Length - 1].attachedRigidbody;
                join1.axis = Vector3.right;
                join1.swingAxis = Vector3.forward;
                join1.swing1Limit = new SoftJointLimit { limit = 15, };
                join1.swing2Limit = new SoftJointLimit { limit = 15, };
                join1.lowTwistLimit = new SoftJointLimit { limit = -15, };
                join1.highTwistLimit = new SoftJointLimit { limit = 5, };
                join1.enablePreprocessing = false;
                join1.massScale = rig.mass;
                join1.connectedMassScale = rig.mass;

                if (!_debug)
                {
                    col.hideFlags = HideFlags.HideInInspector;
                    rig.hideFlags = HideFlags.HideInInspector;
                    join1.hideFlags = HideFlags.HideInInspector;
                }

                var time = _ragdollBones[AvatarTarget.Body].inactiveTime;
                List<Collider> bodyCols = new List<Collider>(_ragdollBones[AvatarTarget.Body].colliders);

                //Neck
                if (neck)
                {
                    var col1 = neck.gameObject.AddComponent<SphereCollider>();
                    col1.radius = head.localPosition.magnitude * 0.5f;
                    col1.center = head.localPosition * 0.5f;
                    //
                    var rig2 = neck.gameObject.AddComponent<Rigidbody>();
                    rig2.isKinematic = initialKineState;
                    rig2.mass = _body ? _body.mass * 0.005f : 1;
                    bodyCols.Add(col1);
                    join1.connectedBody = rig2;
                    //
                    var join2 = neck.gameObject.AddComponent<CharacterJoint>();
                    join2.autoConfigureConnectedAnchor = true;
                    join2.connectedBody = _ragdollBones[AvatarTarget.Body].colliders[_ragdollBones[AvatarTarget.Body].colliders.Length - 1].attachedRigidbody;
                    join2.axis = Vector3.right;
                    join2.swingAxis = Vector3.forward;
                    join2.swing1Limit = new SoftJointLimit { limit = 20, };
                    join2.swing2Limit = new SoftJointLimit { limit = 20, };
                    join2.lowTwistLimit = new SoftJointLimit { limit = -10, };
                    join2.highTwistLimit = new SoftJointLimit { limit = 10, };
                    join2.enablePreprocessing = false;
                    join2.massScale = rig2.mass;
                    join2.connectedMassScale = rig2.mass;
                    if (!_debug)
                    {
                        col1.hideFlags = HideFlags.HideInInspector;
                        rig2.hideFlags = HideFlags.HideInInspector;
                        join2.hideFlags = HideFlags.HideInInspector;
                    }
                }

                bodyCols.Add(col);
                _ragdollBones[AvatarTarget.Body] = (bodyCols.ToArray(), time);
            }
        }

        private void AddSpine(AvatarTarget target, bool isKine, Vector2 hipsSize, Transform last, params Transform[] joints)
        {
            if (joints == null || last == null)
                return;
            List<Collider> spineCols = new List<Collider>();
            for (int i = 0; i < joints.Length; i++)
            {
                if (!joints[i])
                    continue;
                if ((i + 1).InInterval(0, joints.Length))
                {
                    var col1 = joints[i].gameObject.AddComponent<CapsuleCollider>();
                    col1.direction = 1;
                    col1.radius = joints[i + 1].localPosition.magnitude * 0.5f;
                    col1.height = joints[i + 1].localPosition.magnitude;
                    col1.center = new Vector3(0, col1.height * 0.5f, 0);
                    spineCols.Add(col1);
                    //
                    var rig = col1.gameObject.AddComponent<Rigidbody>();
                    rig.isKinematic = isKine;
                    rig.mass = _body ? _body.mass * (0.05f / (joints.Length - 1)) : 1;
                    //
                    var join1 = col1.gameObject.AddComponent<CharacterJoint>();
                    join1.autoConfigureConnectedAnchor = true;
                    join1.connectedBody = i == 0 ? _ragdollBones[AvatarTarget.Root].colliders[0].attachedRigidbody : spineCols[spineCols.Count - 2].attachedRigidbody;
                    join1.axis = Vector3.right;
                    join1.swingAxis = Vector3.up;
                    join1.swing1Limit = new SoftJointLimit { limit = 0, };
                    join1.swing2Limit = new SoftJointLimit { limit = 0, };
                    join1.lowTwistLimit = new SoftJointLimit { limit = 0, };
                    join1.highTwistLimit = new SoftJointLimit { limit = 0, };
                    join1.enablePreprocessing = false;
                    join1.massScale = rig.mass;
                    join1.connectedMassScale = rig.mass;
                    if (!_debug)
                    {
                        col1.hideFlags = HideFlags.HideInInspector;
                        rig.hideFlags = HideFlags.HideInInspector;
                        join1.hideFlags = HideFlags.HideInInspector;
                    }
                }
                else
                {
                    var col1 = joints[i].gameObject.AddComponent<BoxCollider>();
                    col1.size = new Vector3(hipsSize.x * 1.1f, last.localPosition.magnitude, last.localPosition.magnitude * 1.5f);
                    col1.center = new Vector3(0, col1.size.y * 0.5f, 0);
                    spineCols.Add(col1);
                    //
                    var rig = col1.gameObject.AddComponent<Rigidbody>();
                    rig.isKinematic = isKine;
                    rig.mass = _body ? _body.mass * 0.15f : 1;
                    //
                    var join1 = col1.gameObject.AddComponent<CharacterJoint>();
                    join1.autoConfigureConnectedAnchor = true;
                    join1.connectedBody = spineCols[spineCols.Count - 2].attachedRigidbody;
                    join1.axis = Vector3.right;
                    join1.swingAxis = Vector3.up;
                    join1.swing1Limit = new SoftJointLimit { limit = 0, };
                    join1.swing2Limit = new SoftJointLimit { limit = 0, };
                    join1.lowTwistLimit = new SoftJointLimit { limit = 0, };
                    join1.highTwistLimit = new SoftJointLimit { limit = 0, };
                    join1.enablePreprocessing = false;
                    join1.massScale = rig.mass;
                    join1.connectedMassScale = rig.mass;
                    if (!_debug)
                    {
                        col1.hideFlags = HideFlags.HideInInspector;
                        rig.hideFlags = HideFlags.HideInInspector;
                        join1.hideFlags = HideFlags.HideInInspector;
                    }
                }
            }
            _ragdollBones.Add(target, (spineCols.ToArray(), -1));
        }

        private void AddArm(AvatarTarget target, Transform root, Transform middle, Transform tip, bool isKine)
        {
            if (!root || !middle || !tip)
                return;
            var col1 = root.gameObject.AddComponent<CapsuleCollider>();
            col1.direction = 1;
            col1.radius = middle.localPosition.magnitude * 0.25f;
            col1.height = middle.localPosition.magnitude;
            col1.center = new Vector3(0, col1.height * 0.5f, 0);
            //
            var rig = col1.gameObject.AddComponent<Rigidbody>();
            rig.isKinematic = isKine;
            rig.mass = _body ? _body.mass * 0.06f : 1;
            //
            var join1 = col1.gameObject.AddComponent<CharacterJoint>();
            join1.autoConfigureConnectedAnchor = true;
            join1.connectedBody = _ragdollBones[AvatarTarget.Body].colliders[_ragdollBones[AvatarTarget.Body].colliders.Length - 1].attachedRigidbody;
            join1.axis = Vector3.forward;
            join1.swingAxis = Vector3.forward * (target == AvatarTarget.LeftHand ? 1 : -1);
            join1.swing1Limit = new SoftJointLimit { limit = 0, };
            join1.swing2Limit = new SoftJointLimit { limit = 85, };
            join1.lowTwistLimit = new SoftJointLimit { limit = -30, };
            join1.highTwistLimit = new SoftJointLimit { limit = 90, };
            join1.enablePreprocessing = false;
            join1.massScale = rig.mass;
            join1.connectedMassScale = rig.mass;
            if (!_debug)
            {
                col1.hideFlags = HideFlags.HideInInspector;
                rig.hideFlags = HideFlags.HideInInspector;
                join1.hideFlags = HideFlags.HideInInspector;
            }

            /////////////////////

            var col2 = middle.gameObject.AddComponent<CapsuleCollider>();
            col2.direction = 1;
            col2.radius = tip.localPosition.magnitude * 0.35f;
            col2.height = tip.localPosition.magnitude;
            col2.center = new Vector3(0, col2.height * 0.5f, 0);
            //
            var rig2 = col2.gameObject.AddComponent<Rigidbody>();
            rig2.isKinematic = isKine;
            rig2.mass = _body ? _body.mass * 0.04f : 1;
            //
            var join2 = col2.gameObject.AddComponent<CharacterJoint>();
            join2.autoConfigureConnectedAnchor = true;
            join2.connectedBody = rig;
            join2.axis = Vector3.forward;
            join2.swingAxis = Vector3.up * (target == AvatarTarget.LeftHand ? 1 : -1);
            join2.swing1Limit = new SoftJointLimit { limit = 25, };
            join2.swing2Limit = new SoftJointLimit { limit = 0, };
            join2.lowTwistLimit = new SoftJointLimit { limit = 0, };
            join2.highTwistLimit = new SoftJointLimit { limit = 90, };
            join2.enablePreprocessing = false;
            join2.massScale = rig2.mass;
            join2.connectedMassScale = rig2.mass;
            if (!_debug)
            {
                col2.hideFlags = HideFlags.HideInInspector;
                rig2.hideFlags = HideFlags.HideInInspector;
                join2.hideFlags = HideFlags.HideInInspector;
            }

            _ragdollBones.Add(target, (new[] { col1, col2 }, -1));
        }

        private void AddLeg(AvatarTarget target, Transform root, Transform middle, Transform tip, bool isKine)
        {
            if (!root || !middle || !tip)
                return;
            var col1 = root.gameObject.AddComponent<CapsuleCollider>();
            col1.direction = 1;
            col1.radius = middle.localPosition.magnitude * 0.155f;
            col1.height = middle.localPosition.magnitude;
            col1.center = new Vector3(0, col1.height * 0.5f, 0);
            //
            var rig = col1.gameObject.AddComponent<Rigidbody>();
            rig.isKinematic = isKine;
            rig.mass = _body ? _body.mass * 0.145f : 1;
            //
            var join1 = col1.gameObject.AddComponent<CharacterJoint>();
            join1.autoConfigureConnectedAnchor = true;
            join1.connectedBody = _ragdollBones[AvatarTarget.Root].colliders[0].attachedRigidbody;
            join1.axis = Vector3.right * -1;
            join1.swingAxis = Vector3.forward;
            join1.swing1Limit = new SoftJointLimit { limit = 30, };
            join1.swing2Limit = new SoftJointLimit { limit = 0, };
            join1.lowTwistLimit = new SoftJointLimit { limit = -20, };
            join1.highTwistLimit = new SoftJointLimit { limit = 70, };
            join1.enablePreprocessing = false;
            join1.massScale = rig.mass;
            join1.connectedMassScale = rig.mass;
            if (!_debug)
            {
                col1.hideFlags = HideFlags.HideInInspector;
                rig.hideFlags = HideFlags.HideInInspector;
                join1.hideFlags = HideFlags.HideInInspector;
            }

            /////////////////////

            var col2 = middle.gameObject.AddComponent<CapsuleCollider>();
            col2.direction = 1;
            col2.radius = tip.localPosition.magnitude * 0.25f;
            col2.height = tip.localPosition.magnitude;
            col2.center = new Vector3(0, col2.height * 0.5f, 0);
            //
            var rig2 = col2.gameObject.AddComponent<Rigidbody>();
            rig2.isKinematic = isKine;
            rig2.mass = _body ? _body.mass * 0.08f : 1;
            //
            var join2 = col2.gameObject.AddComponent<CharacterJoint>();
            join2.autoConfigureConnectedAnchor = true;
            join2.connectedBody = rig;
            join2.axis = Vector3.right * -1;
            join2.swingAxis = Vector3.forward;
            join2.swing1Limit = new SoftJointLimit { limit = 0, };
            join2.swing2Limit = new SoftJointLimit { limit = 0, };
            join2.lowTwistLimit = new SoftJointLimit { limit = -80, };
            join2.highTwistLimit = new SoftJointLimit { limit = 0, };
            join2.enablePreprocessing = false;
            join2.massScale = rig2.mass;
            join2.connectedMassScale = rig2.mass;
            if (!_debug)
            {
                col2.hideFlags = HideFlags.HideInInspector;
                rig2.hideFlags = HideFlags.HideInInspector;
                join2.hideFlags = HideFlags.HideInInspector;
            }

            _ragdollBones.Add(target, (new[] { col1, col2 }, -1));
        }



        private void UpdateAutoSwitch(float delta)
        {
            if (_ragdollBones == null)
                return;
            for (int i = 0; i < _ragdollBones.Count; i++)
            {
                var part = _ragdollBones.ElementAt(i);
                if (part.Value.colliders != null)
                {
                    float time = _ragdollBones[part.Key].inactiveTime;
                    if (time > 0)
                    {
                        time -= delta;
                        if (time <= 0)
                        {
                            SwitchRagDollMode(true, false, part.Key);
                        }
                    }
                    _ragdollBones[part.Key] = (_ragdollBones[part.Key].colliders, time);
                }
            }
            return;
        }

        #endregion
        #region Flow ###################################################################

        private void OnEnable()
        {
            CreateRagdoll();
        }

        private void Update()
        {
            if (!_animator)
            {
                _animator = GetComponent<Animator>();
                _body = GetComponent<Rigidbody>();
                if (_animator)
                    CreateRagdoll();
                return;
            }
            float delta = Time.deltaTime;
            UpdateAutoSwitch(delta);
        }

        private void OnDisable()
        {
            DestroyRagdoll();
            _animator = null;
            _body = null;
        }

        #endregion
    }
}