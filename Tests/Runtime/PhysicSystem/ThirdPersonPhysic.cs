using System;
using System.Collections.Generic;
using UnityEngine;

namespace PulseEngine.PhysicSystem
{
    /// <summary>
    /// Handle 3rd Person Physics
    /// </summary>
    [System.Serializable]
    public class ThirdPersonPhysic : PulseModuleFeature
    {
        #region Variables ##############################################

        private Transform _parentTransform;
        private (Vector3 charLastFramePos, Quaternion charLastFrameRot, Vector3 lastFrameGravityDir, Vector3 lastFrameSurfacePos, Vector3 parentOffset) _parentConstraintParams;
        private bool _cannotClampPosition;
        private float _gravityTimer;

        #endregion

        #region Events ##############################################

        #endregion

        #region Properties ##############################################

        #endregion

        #region Publics Methods ###########################################

        /// <summary>
        /// Reset the gravity curve's evaluated value.
        /// </summary>
        public void ResetGravityTimer()
        {
            _gravityTimer = 0;
        }

        public override void OnInit()
        {
            base.OnInit();
            if (_parent == null)
                return;
            _parentTransform = new GameObject($"{_parent.name}_PhysicSnapper").transform;
            _parentTransform.SetParent(_parent.transform);
        }

        public override void OnActivate()
        {
            base.OnActivate();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            PhysicSystemComponent phyCompSys = _parent as PhysicSystemComponent;
            if (phyCompSys == null)
                return;
            phyCompSys.LastPhysicSpace = phyCompSys.CurrentPhysicSpace;
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (_parent == null)
                return;
            PhysicSystemComponent phyCompSys = _parent as PhysicSystemComponent;
            if (phyCompSys == null)
                return;
            float delta = Time.fixedDeltaTime;
            phyCompSys.CurrentPhysicSpace = PhysicSpace.unSpecified;
            if (phyCompSys._rigidbody)
            {
                phyCompSys._rigidbody.drag = 0;
                //Debug.DrawRay(CenterOfMass, _rigidbody.velocity.normalized, Color.magenta);
            }
            SurfaceCheck(delta);


            if (phyCompSys.CurrentPhysicSpace == PhysicSpace.unSpecified)
            {
                phyCompSys.AirTime += delta;
                _gravityTimer += delta;
                phyCompSys.SurfaceSnapPoint = Vector3.negativeInfinity;
                phyCompSys.CurrentPhysicSpace = PhysicSpace.inAir;
                if (_parentTransform)
                    _parentTransform.SetParent(_parent.transform);
            }
            else
            {
                if (phyCompSys.AirTime > 0)
                    phyCompSys.AirTime = 0;
                if (_gravityTimer > 0)
                    _gravityTimer = 0;
            }
            ApplyGravity(delta);
        }

        public override void OnLateUpdate()
        {
            base.OnLateUpdate();
        }

        public override void OnDrawGizmo(bool selected)
        {
            PhysicSystemComponent phyCompSys = _parent as PhysicSystemComponent;
            if (phyCompSys == null)
                return;
            base.OnDrawGizmo(selected);
            if (_parentTransform && selected)
            {
                Gizmos.color = phyCompSys.CurrentPhysicSpace == PhysicSpace.onGround ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(_parentTransform.position, Vector3.one * 0.2f);
            }
        }

        #endregion

        #region Private Methods ###########################################


        /// <summary>
        /// Apply gravity
        /// </summary>
        private void ApplyGravity(float delta)
        {
            if (_parent == null)
                return;
            PhysicSystemComponent phyCompSys = _parent as PhysicSystemComponent;
            if (phyCompSys == null)
                return;

            if (!phyCompSys._rigidbody)
                return;
            if (phyCompSys._rigidbody.useGravity)
                phyCompSys._rigidbody.useGravity = false;
            Vector3 globalGravityDir = Physics.gravity.normalized;

            float gravityIntensity = Mathf.Abs(Physics.gravity.magnitude * phyCompSys.GravityMultiplier);
            phyCompSys.CurrentGravityDirection = Vector3.Lerp(globalGravityDir, phyCompSys.DesiredGravityDir.normalized, phyCompSys.DesiredGravityDir.magnitude);
            Vector3 projectedVelocity = Vector3.Project(phyCompSys._rigidbody.velocity, phyCompSys.CurrentGravityDirection.normalized);
            PulseDebug.DrawRay(phyCompSys.CenterOfMass, phyCompSys.CurrentGravityDirection * 0.2f, Color.cyan);

            if (phyCompSys.CurrentPhysicSpace != PhysicSpace.inAir)
                return;

            if (phyCompSys.GravityCurve != null && projectedVelocity.magnitude < gravityIntensity)
                phyCompSys._rigidbody.AddForce(phyCompSys.CurrentGravityDirection * gravityIntensity * phyCompSys.GravityCurve.Evaluate(Mathf.Clamp01(_gravityTimer * 1.5f)), ForceMode.Acceleration);
        }

        /// <summary>
        /// Check if on a surface
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SurfaceCheck(float deltaTime)
        {
            if (_parent == null)
                return;
            PhysicSystemComponent phyCompSys = _parent as PhysicSystemComponent;
            if (phyCompSys == null)
                return;

            float maxSurfaceDistance = 20;
            float maxDistance = (phyCompSys.transform.position - phyCompSys.CenterOfMass).magnitude;
            phyCompSys.CurrentSurfaceNormal = -phyCompSys.CurrentGravityDirection;
            phyCompSys.SurfaceDistance = maxSurfaceDistance;

            //Restrict current gravity direction inside a cone
            Vector3 coneEdge = Vector3.forward * phyCompSys.ColliderWidth + Vector3.up * maxDistance;
            float coneAngle = Vector3.Angle(coneEdge, Vector3.up);
            float coneSinus = Mathf.Sin(coneAngle * Mathf.Deg2Rad);
            Vector3 positiveCurrentGravityDir = new Vector3(Mathf.Abs(phyCompSys.CurrentGravityDirection.x), Mathf.Abs(phyCompSys.CurrentGravityDirection.y), Mathf.Abs(phyCompSys.CurrentGravityDirection.z));
            Vector3 signsOfCurrentGravityDir = new Vector3(Mathf.Sign(phyCompSys.CurrentGravityDirection.x), Mathf.Sign(phyCompSys.CurrentGravityDirection.y), Mathf.Sign(phyCompSys.CurrentGravityDirection.z));
            Vector3 insideShapeVector = (Vector3.forward * Tools.ThresholdSwithcher(positiveCurrentGravityDir.z, maxDistance * phyCompSys.CurrentGravityDirection.z, phyCompSys.ColliderWidth * signsOfCurrentGravityDir.z, coneSinus)
                + Vector3.right * Tools.ThresholdSwithcher(positiveCurrentGravityDir.x, maxDistance * phyCompSys.CurrentGravityDirection.x, phyCompSys.ColliderWidth * signsOfCurrentGravityDir.x, coneSinus)
                + Vector3.up * maxDistance * phyCompSys.CurrentGravityDirection.y);

            //Determine what physic space to put on
            PhysicSpace spaceToGo = PhysicSpace.onGround;
            float xzMax = Mathf.Max(Mathf.Abs(Vector3.Dot(phyCompSys.CurrentGravityDirection, phyCompSys.transform.right)), Mathf.Abs(Vector3.Dot(phyCompSys.CurrentGravityDirection, phyCompSys.transform.forward)));
            float dotY = Mathf.Abs(Vector3.Dot(phyCompSys.CurrentGravityDirection, phyCompSys.transform.up));
            if (xzMax > dotY && xzMax > 0.7f)
                spaceToGo = PhysicSpace.onWall;

            //Proceed to detection
            float distance = insideShapeVector.magnitude * 1.1f;
            Vector3 insideShapeDir = insideShapeVector.normalized;
            Vector3 fromPosOffset = phyCompSys.transform.position - (phyCompSys.CenterOfMass + insideShapeVector);
            float colliderAdjustedRadius = phyCompSys.ColliderWidth * 0.5f;
            PulseDebug.DrawRay(phyCompSys.CenterOfMass, insideShapeDir * distance, Color.green);
            PulseDebug.DrawRay((phyCompSys.CenterOfMass + insideShapeVector), fromPosOffset, Color.yellow);
            if (Physics.SphereCast(phyCompSys.CenterOfMass, colliderAdjustedRadius, insideShapeDir, out var hit, distance, phyCompSys.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                phyCompSys.SurfaceDistance = 0;

                if (Physics.Raycast(phyCompSys.CenterOfMass, insideShapeDir, distance + colliderAdjustedRadius * 1.1f, phyCompSys.GroundLayer, QueryTriggerInteraction.Ignore))
                    phyCompSys.CurrentSurfaceNormal = hit.normal;

                //Calculate sphere shape cast offset hit adjustements
                Vector3 have = hit.point - phyCompSys.CenterOfMass;
                Vector3 mustHave = insideShapeDir * (distance);
                Vector3 hitPointOffset = (phyCompSys.CenterOfMass + Vector3.Project(have, insideShapeDir)) - hit.point;
                Vector3 offsetHitPoint = hit.point + hitPointOffset;
                PulseDebug.DrawCircle(hit.point, 0.3f, hit.normal, Color.yellow);
                PulseDebug.DrawCircle(offsetHitPoint, 0.3f, hit.normal, Color.green);

                if (have.sqrMagnitude <= mustHave.sqrMagnitude)
                {
                    if (phyCompSys.LastPhysicSpace == PhysicSpace.inAir)
                    {
                        phyCompSys.TriggerSpaceChange(PhysicSpace.onGround);
                        //Landing
                        if (phyCompSys._rigidbody)
                        {
                            Vector3 planarMotion = Vector3.ProjectOnPlane(phyCompSys._rigidbody.velocity, phyCompSys.CurrentGravityDirection);
                            phyCompSys._rigidbody.velocity = planarMotion;
                        }
                    }
                    if (_parentTransform)
                    {
                        if (_parentTransform.parent != hit.transform)
                        {
                            _parentTransform.SetParent(hit.transform);
                            _parentTransform.position = offsetHitPoint;
                            _parentTransform.rotation = phyCompSys.transform.rotation;
                            _parentConstraintParams.lastFrameSurfacePos = _parentTransform.parent.position;
                        }
                        else
                        {
                            Vector3 parentDisplacement = _parentTransform.parent.position - _parentConstraintParams.lastFrameSurfacePos;
                            if ((phyCompSys._rigidbody && phyCompSys._rigidbody.velocity.sqrMagnitude > 1) || _parentConstraintParams.lastFrameGravityDir != phyCompSys.CurrentGravityDirection)
                            {
                                _parentTransform.position = offsetHitPoint + parentDisplacement;
                                _parentTransform.rotation = _parent.transform.rotation;
                            }
                            if (_parentConstraintParams.charLastFrameRot != phyCompSys.transform.rotation)
                            {
                            }
                            phyCompSys.SurfaceSnapPoint = _parentTransform.position + fromPosOffset;
                            phyCompSys.SurfaceSnapRot = _parentTransform.rotation;
                            PulseDebug.DrawCircle(phyCompSys.SurfaceSnapPoint, 0.5f, hit.normal, Color.yellow);
                        }
                    }
                    phyCompSys.CurrentPhysicSpace = spaceToGo;
                }
            }
            else if (Physics.SphereCast(phyCompSys.CenterOfMass, colliderAdjustedRadius, insideShapeDir, out var hit2, maxSurfaceDistance, phyCompSys.GroundLayer, QueryTriggerInteraction.Ignore))
            {
                phyCompSys.SurfaceDistance = Vector3.Distance(phyCompSys.transform.position, hit2.point);
            }
            _parentConstraintParams.charLastFramePos = phyCompSys.transform.position;
            _parentConstraintParams.charLastFrameRot = phyCompSys.transform.rotation;
            _parentConstraintParams.lastFrameGravityDir = phyCompSys.CurrentGravityDirection;
            if (_parentTransform && _parentTransform.parent)
            {
                _parentConstraintParams.lastFrameSurfacePos = _parentTransform.parent.position;
                _parentConstraintParams.parentOffset = phyCompSys.transform.position - _parentTransform.position;
            }

            phyCompSys.CurrentPhysicSpace = phyCompSys.CurrentPhysicSpace;
        }

        #endregion
    }
}