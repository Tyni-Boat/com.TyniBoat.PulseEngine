using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PulseEngine;
using UnityEngine.InputSystem;
using PulseEngine.Animancer;
using PulseEngine.CharacterControl;
using PulseEngine.UI;

[RequireComponent(typeof(PlayerInput))]
public class KinematicControllerTester3d : MonoBehaviour
{
    #region Constants #############################################################

    #endregion

    #region Variables #############################################################

    public AvatarMask mask;
    public AnimaMotion idle;
    public AnimaMotion midAir;
    public AnimaMotion landing;
    public AnimaMotion motion;
    public AnimaMotion dash;
    public AnimaMotion Jump;
    public AnimaMotion block;


    private KinematicController3d _kontroller;
    private PhysicCharacterController _controller;
    private AnimancerMachine _animancer;

    private PlayerInput _playerInput;

    private Vector2 _moveVec = Vector2.zero;

    private bool _defense;

    private float _jumpRequestTime = 0;

    #endregion

    #region Statics   #############################################################

    #endregion

    #region Inner Types ###########################################################

    #endregion

    #region Properties ############################################################

    #endregion

    #region Public Functions ######################################################

    #endregion

    #region Private Functions #####################################################

    #endregion

    #region Jobs      #############################################################

    #endregion

    #region MonoBehaviours ########################################################

    private void OnEnable()
    {
        _kontroller = GetComponent<KinematicController3d>();
        _controller = GetComponent<PhysicCharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _animancer = GetComponent<AnimancerMachine>();

        _playerInput.onActionTriggered += PlayerActionTrigerred;
        //_kontroller.OnSurfaceContactChanged += OnLanding;
        _controller.OnSurfaceContact += OnLanding;
    }

    private void OnLanding(object sender, SurfaceInformations e)
    {
        if (!ReferenceEquals(sender, _controller))
            return;
        if (_controller.CurrentSurface.SurfaceType != SurfaceType.Ground)
            return;
        if (_controller.CurrentPhysicSpace != PhysicSpace.onGround)
            return;
        if (_controller.AirTime <= 0.1f)
            return;
        playerHealth -= _controller.AirTime * 10;
        UIManager.GetHUD<HealthHudTest>()?.UpdateHealth(playerHealth);
        _animancer?.PlayOnce(landing, null, true);
    }

    private void OnDisable()
    {
        if (_playerInput)
            _playerInput.onActionTriggered -= PlayerActionTrigerred;
        //_kontroller.OnSurfaceContactChanged -= OnLanding;
        _controller.OnSurfaceContact -= OnLanding;
    }

    private void PlayerActionTrigerred(InputAction.CallbackContext obj)
    {
        if (obj.action.type == InputActionType.Value && obj.action.expectedControlType == typeof(Vector2).Name)
        {
            if (obj.action.name == "Move")
                _moveVec = obj.action.ReadValue<Vector2>();
            //if (obj.action.name == "Camera")
            //    CamMoveVec = obj.action.ReadValue<Vector2>();
        }
        if (obj.action.name == "Jump" && obj.action.type == InputActionType.Button && obj.action.phase == InputActionPhase.Performed)
        {
            //if (animancer && _kontroller.CurrentPhysicSpace == PhysicSpace.onGround)
            //{
            //    if (animancer.PlayOnce(Jump, () =>
            //    {
            //        if (_kontroller.CurrentSurface != null)
            //            return new Vector2(Mathf.InverseLerp(0, 2, _kontroller.SurfaceDistance), 0);
            //        return new Vector2(0, 0);
            //    }, animancer.IsPlayingFullBody(motion)))
            //    {
            //        _kontroller?.JumpForHeight(2);
            //    }
            //}
            if (_controller.CanJump)
            {
                if (_animancer && _controller.AirTime < 0.15f)
                {
                    //jump
                    if (_animancer.PlayOnce(Jump, () =>
                    {
                        float paramHeight = Mathf.InverseLerp(0, 4, _controller.SurfaceDistance);
                        return new Vector2(paramHeight, 0);
                    }, _animancer.IsPlayingFullBody(motion)))
                    {
                        _controller?.JumpForHeight(4);
                    }
                }
                else if (_controller.CurrentPhysicSpace == PhysicSpace.inAir)
                {
                    _jumpRequestTime = 0.25f;
                }
            }
        }
        if (obj.action.name == "Crouch" && obj.action.type == InputActionType.Button)
        {
            if (obj.action.phase == InputActionPhase.Started)
            {
                if (_controller.CurrentPhysicSpace == PhysicSpace.onGround)
                    _animancer?.PlayOnce(dash);
            }
            if (obj.action.phase == InputActionPhase.Canceled)
            {
                //_controller?.Crouch(false);
            }
        }
        if (obj.action.name == "Interraction" && obj.action.type == InputActionType.Button)
        {
            if (obj.action.phase == InputActionPhase.Started)
            {
                _defense = true;
                _controller?.IgnoreCollisionMode(true);
            }
            if (obj.action.phase == InputActionPhase.Canceled)
            {
                _defense = false;
                _controller?.IgnoreCollisionMode(false);
            }
        }
    }

    private void UpdatePhysic(float delta)
    {
        if (_jumpRequestTime > 0 && _animancer.IsPlayingFullBody(landing))
        {
            if (_animancer.PlayOnce(Jump, () =>
            {
                float paramHeight = Mathf.InverseLerp(0, 2, _controller.SurfaceDistance);
                return new Vector2(paramHeight, 0);
            },false, 1))
            {
                _jumpRequestTime = 0;
                _controller?.JumpForHeight(2);
            }
        }
        //
        _animancer.PlayWhile(idle, () => _moveVec.sqrMagnitude <= 0 && _controller.CurrentPhysicSpace == PhysicSpace.onGround);
        //
        _animancer.PlayWhile(Jump, () => _controller.CurrentPhysicSpace == PhysicSpace.inAir && _controller.AirTime > 0.15f && _animancer.CurrentMotionCanTransition, () =>
        {
            if (_controller.CurrentSurface.surfaceCollider)
                return new Vector2(Mathf.InverseLerp(0, 2, _controller.SurfaceDistance), 0);
            return new Vector2(0, 0);
        });
        //
        _animancer.PlayWhile(motion, () => _moveVec.magnitude > 0 && _controller.CurrentPhysicSpace == PhysicSpace.onGround, () => new Vector2(_moveVec.magnitude, 0));
        //
        _animancer.MaskPlayWhile(block, () => _defense && _controller.CurrentPhysicSpace == PhysicSpace.onGround, mask);
        //
        if (_animancer.IsPlayingFullBody(dash))
        {
            _controller?.AdjustShape(0.5f, 0.5f, 0.15f);
        }
        else
        {
            _controller?.AdjustShape(1.8f, 0.5f, 0.5f);
        }
        //
        _controller?.Move(_animancer.Velocity, _animancer.Velocity.magnitude, 0);
        ///
        if (_controller.CurrentPhysicSpace == PhysicSpace.onGround && _animancer.IsPlayingFullBody(motion))
        {
            if (_moveVec.sqrMagnitude > 0)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane((Camera.main.transform.forward * _moveVec.y + Camera.main.transform.right * _moveVec.x), transform.up)), delta * 15);
        }
        else if (_controller.CurrentPhysicSpace == PhysicSpace.inAir)
        {
            _controller?.Move(Vector3.ProjectOnPlane((Camera.main.transform.forward * _moveVec.y + Camera.main.transform.right * _moveVec.x), transform.up), 3, 5);
        }
    }

    private void UpdateKinematic()
    {
        _animancer.PlayWhile(idle, () => _moveVec.sqrMagnitude <= 0 && _kontroller.CurrentPhysicSpace == PhysicSpace.onGround && _kontroller.CurrentSurface.NoGravityForce);
        //
        _animancer.PlayWhile(Jump, () => _kontroller.CurrentPhysicSpace == PhysicSpace.inAir && _animancer.CurrentMotionCanTransition && _kontroller.AirTime > 0.15f, () =>
        {
            if (_kontroller.CurrentSurface != null)
                return new Vector2(Mathf.InverseLerp(0, 2, _kontroller.SurfaceDistance), 0);
            return new Vector2(0, 0);
        });
        //
        _animancer.PlayWhile(motion, () => _moveVec.magnitude > 0 && _kontroller.CurrentPhysicSpace == PhysicSpace.onGround && _kontroller.CurrentSurface.NoGravityForce, () => new Vector2(_moveVec.magnitude, 0));
        //
        _animancer.MaskPlayWhile(block, () => _defense && _kontroller.CurrentPhysicSpace == PhysicSpace.onGround, mask);
        //
        _kontroller?.ApplyMovement(_animancer.Velocity);
        ///
        if (_kontroller.CurrentPhysicSpace == PhysicSpace.onGround && _animancer.IsPlayingFullBody(motion))
        {
            _kontroller?.LookFromInputs(Camera.main.transform, _moveVec, 15);
        }
        else if (_kontroller.CurrentPhysicSpace == PhysicSpace.inAir)
        {
            _kontroller?.ApplyMovement((Camera.main.transform.forward * _moveVec.y + Camera.main.transform.right * _moveVec.x) * 10, 15);
        }
    }

    Vector3 pt = Vector3.zero;
    Vector3 n = Vector3.zero;
    private void Update()
    {
        float d = Time.deltaTime;
        if (_jumpRequestTime > 0)
            _jumpRequestTime -= d;
        if (_animancer)
        {
            UpdatePhysic(d);
        }
    }

    float playerHealth = 100;

    private void OnGUI()
    {
        if (UIManager.WindowStackCount <= 0)
        {
            var healthbar = UIManager.GetHUD<HealthHudTest>();
            if (healthbar == null || !healthbar.gameObject.activeSelf)
            {
                UIManager.ShowHUD<HealthHudTest>();
                UIManager.GetHUD<HealthHudTest>()?.UpdateHealth(playerHealth);
            }
            var heightBar = UIManager.GetHUD<HeightMeterHUD>();
            if (heightBar == null || !heightBar.gameObject.activeSelf)
            {
                UIManager.ShowHUD<HeightMeterHUD>();
                UIManager.GetHUD<HeightMeterHUD>()?.UpdateHeight(_kontroller.SurfaceDistance);
            }
            if (GUILayout.Button("Open wins"))
            {
                UIManager.HideHUD<HealthHudTest>();
                UIManager.HideHUD<HeightMeterHUD>();
                UIManager.OpenWindow<WindowTest_1>();
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        Debug.Log($"{this.name} Collided {other.collider.name}");
    }

    #endregion
}

