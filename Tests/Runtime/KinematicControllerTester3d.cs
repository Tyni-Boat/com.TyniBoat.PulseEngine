using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PulseEngine;
using UnityEngine.InputSystem;


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


    private KinematicController3d _controller;
    private AnimancerMachine animancer;

    private PlayerInput _playerInput;

    private Vector2 moveVec = Vector2.zero;

    private bool _defense;

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
        _controller = GetComponent<KinematicController3d>();
        _playerInput = GetComponent<PlayerInput>();
        animancer = GetComponent<AnimancerMachine>();

        _playerInput.onActionTriggered += PlayerActionTrigerred;
        _controller.OnSurfaceContactChanged += OnLanding;
    }

    private void OnLanding(object sender, bool e)
    {
        if (!ReferenceEquals(sender, _controller))
            return;
        if (!e)
            return;
        if (_controller.AirTime <= 0.25f)
            return;
        animancer?.PlayOnce(landing, null, true);
        playerHealth -= _controller.AirTime * 10;
        UIManager.GetHUD<HealthHudTest>()?.UpdateHealth(playerHealth);
    }

    private void OnDisable()
    {
        if (_playerInput)
            _playerInput.onActionTriggered -= PlayerActionTrigerred;
        _controller.OnSurfaceContactChanged -= OnLanding;
    }

    private void PlayerActionTrigerred(InputAction.CallbackContext obj)
    {
        if (obj.action.type == InputActionType.Value && obj.action.expectedControlType == typeof(Vector2).Name)
        {
            if (obj.action.name == "Move")
                moveVec = obj.action.ReadValue<Vector2>();
            //if (obj.action.name == "Camera")
            //    CamMoveVec = obj.action.ReadValue<Vector2>();
        }
        if (obj.action.name == "Jump" && obj.action.type == InputActionType.Button && obj.action.phase == InputActionPhase.Performed)
        {
            if (animancer && _controller.CurrentPhysicSpace == PhysicSpace.onGround)
            {
                if (animancer.PlayOnce(Jump, () =>
                {
                    if (_controller.CurrentSurface != null)
                        return new Vector2(Mathf.InverseLerp(0, 2, _controller.SurfaceDistance), 0);
                    return new Vector2(0, 0);
                }, animancer.IsPlayingFullBody(motion)))
                {
                    _controller?.JumpForHeight(2);
                }
            }
        }
        if (obj.action.name == "Crouch" && obj.action.type == InputActionType.Button)
        {
            if (obj.action.phase == InputActionPhase.Started)
            {
                //_controller?.Crouch(true);
                animancer?.PlayOnce(dash);
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
                //_controller?.NoClipState(true);
            }
            if (obj.action.phase == InputActionPhase.Canceled)
            {
                _defense = false;
                //_controller?.NoClipState(false);
            }
        }
    }

    Vector3 pt = Vector3.zero;
    Vector3 n = Vector3.zero;
    private void Update()
    {
        if (animancer)
        {
            animancer.PlayWhile(idle, () => moveVec.sqrMagnitude <= 0 && _controller.CurrentPhysicSpace == PhysicSpace.onGround && _controller.CurrentSurface.NoGravityForce);
            animancer.PlayWhile(Jump, () => _controller.CurrentPhysicSpace == PhysicSpace.inAir && animancer.CurrentMotionCanTransition && _controller.AirTime > 0.15f, () =>
            {
                if (_controller.CurrentSurface != null)
                    return new Vector2(Mathf.InverseLerp(0, 2, _controller.SurfaceDistance), 0);
                return new Vector2(0, 0);
            });
            animancer.PlayWhile(motion, () => moveVec.magnitude > 0 && _controller.CurrentPhysicSpace == PhysicSpace.onGround && _controller.CurrentSurface.NoGravityForce, () => new Vector2(moveVec.magnitude, 0));
            animancer.MaskPlayWhile(block, () => _defense && _controller.CurrentPhysicSpace == PhysicSpace.onGround, mask);
            _controller?.ApplyMovement(animancer.Velocity);
        }
        if (_controller.CurrentPhysicSpace == PhysicSpace.onGround && animancer.IsPlayingFullBody(motion))
        {
            _controller?.LookFromInputs(Camera.main.transform, moveVec, 15);
        }
        else if (_controller.CurrentPhysicSpace == PhysicSpace.inAir)
        {
            _controller?.ApplyMovement((Camera.main.transform.forward * moveVec.y + Camera.main.transform.right * moveVec.x) * 10, 15);
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
                UIManager.GetHUD<HeightMeterHUD>()?.UpdateHeight(_controller.SurfaceDistance);
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

