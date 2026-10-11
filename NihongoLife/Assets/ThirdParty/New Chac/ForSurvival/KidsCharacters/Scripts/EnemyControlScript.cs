using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class EnemyControlScript : MonoBehaviour
{
    private Animator _Animator;
    private CharacterController _Ctrl;
    private Vector3 _MoveDirection = Vector3.zero;
    private GameObject _View_Camera;
    private Transform _Light;
    private SkinnedMeshRenderer _MeshRenderer;
    [SerializeField] private GameObject _Mesh;
    [SerializeField] private GameObject _Hand_L;
    [SerializeField] private GameObject _Hand_R;
    // Hash
    private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
    private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.jump");
    private static readonly int LandingState = Animator.StringToHash("Base Layer.landing");
    private static readonly int DamageState = Animator.StringToHash("Base Layer.damage");
    private static readonly int StunState = Animator.StringToHash("Base Layer.stun");
    private static readonly int AttackChargeState = Animator.StringToHash("Base Layer.attack_charge");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.attack");
    private static readonly int AttackBackState = Animator.StringToHash("Base Layer.attack_back");
    private static readonly int CatchChargeState = Animator.StringToHash("Base Layer.catch_charge");
    private static readonly int CatchSuccessState = Animator.StringToHash("Base Layer.catch_success");
    private static readonly int CatchSuccessBackState = Animator.StringToHash("Base Layer.catch_success_back");
    private static readonly int CatchMissState = Animator.StringToHash("Base Layer.catch_miss");
    private static readonly int CatchMissBackState = Animator.StringToHash("Base Layer.catch_miss_back");
    private static readonly int CatchLetgoState = Animator.StringToHash("Base Layer.catch_letgo");
    private static readonly int CatchLoopState = Animator.StringToHash("Base Layer.catch_loop");
    private static readonly int PushState = Animator.StringToHash("Base Layer.push");
    private static readonly int Telekinesis1State = Animator.StringToHash("Base Layer.telekinesis1");
    private static readonly int Telekinesis2State = Animator.StringToHash("Base Layer.telekinesis2");
    private static readonly int AppraiseState = Animator.StringToHash("Base Layer.appraise");
    private static readonly int LookbackState = Animator.StringToHash("Base Layer.lookback");
    private static readonly int PeekState = Animator.StringToHash("Base Layer.peek");
    private static readonly int PanicState = Animator.StringToHash("Base Layer.panic");
    private static readonly int CowardState = Animator.StringToHash("Base Layer.coward");
    private static readonly int CaughtState = Animator.StringToHash("Base Layer.caught");
    private static readonly int CaughtDownState = Animator.StringToHash("Base Layer.caught_down");
    private static readonly int FearState = Animator.StringToHash("Base Layer.fear");

    private static readonly int JumpTag = Animator.StringToHash("Jump");
    private static readonly int DamageTag = Animator.StringToHash("Damage");
    private static readonly int StunTag = Animator.StringToHash("Stun");
    private static readonly int AttackTag = Animator.StringToHash("Attack");
    private static readonly int CatchTag = Animator.StringToHash("Catch");
    private static readonly int PushTag = Animator.StringToHash("Push");

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int JumpPoseParameter = Animator.StringToHash("JumpPose");
    private static readonly int IdlePoseParameter = Animator.StringToHash("IdlePose");
    private static readonly int SneakParameter = Animator.StringToHash("Sneak");
    private static readonly int CatchParameter = Animator.StringToHash("Catch");
    private static readonly int StunParameter = Animator.StringToHash("Stun");
    private static readonly int LRParameter = Animator.StringToHash("LR");
    private static readonly int CaughtPoseParameter = Animator.StringToHash("CaughtPose");

    private GameObject _Target;
    private Animator _TargetAnimator;
    private bool _WithinRange = false;

    
    void Start()
    {
        _Animator = this.GetComponent<Animator>();
        _Ctrl = this.GetComponent<CharacterController>();
        _View_Camera = GameObject.Find("Main Camera");
        _Light = GameObject.Find("Directional Light").transform;
        _MeshRenderer = _Mesh.GetComponent<SkinnedMeshRenderer>();
        _Target = GameObject.Find("BoyNPC2");
        _TargetAnimator = _Target.GetComponent<Animator>();
    }

    
    void Update()
    {
        CAMERA();
        DIRECTION_LIGHT();
        GRAVITY();
        STATUS();
        LR();

        if(!_Status.ContainsValue( true ))
        {
            MOVE();
            JUMP();
            DAMAGE();
            STUN();
            TELEKINESIS();
            LOOKBACK();
            PEEK();
            ATTACK();
            PUSH();
            CATCH();
            APPRAISE();
        }
        else if(_Status.ContainsValue( true ))
        {
            int status_name = 0;
            foreach(var i in _Status)
            {
                if(i.Value == true)
                {
                    status_name = i.Key;
                    break;
                }
            }
            if(status_name == Jump)
            {
                MOVE();
                JUMP();
            }
            else if(status_name == Damage)
            {
                DAMAGE();
            }
            else if(status_name == Stun)
            {
                STUN();
            }
            else if(status_name == Attack)
            {
                ATTACK();
            }
            else if(status_name == Push)
            {
                PUSH();
            }
            else if(status_name == Catch)
            {
                CATCH();
            }
        }
    }
    //--------------------------------------------------------------------- STATUS
    // Flags to control slime's action
    // It is used by method in Update()
    //---------------------------------------------------------------------
    private const int Jump = 1;
    private const int Damage = 2;
    private const int Stun = 3;
    private const int Attack = 4;
    private const int Push = 5;
    private const int Catch = 6;

    private Dictionary<int, bool> _Status = new Dictionary<int, bool>
    {
        {Jump, false },
        {Damage, false },
        {Stun, false },
        {Attack, false },
        {Push, false },
        {Catch, false },
    };
    //------------------------------
    private void STATUS ()
    {
        if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == JumpTag)
        {
            _Status[Jump] = true;
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag)
        {
            _Status[Jump] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == DamageTag)
        {
            _Status[Damage] = true;
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != DamageTag)
        {
            _Status[Damage] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == StunTag)
        {
            _Status[Stun] = true;
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != StunTag)
        {
            _Status[Stun] = false;
        }
        
        if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == PushTag)
        {
            _Status[Push] = true;
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != PushTag)
        {
            _Status[Push] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == CatchTag)
        {
            _Status[Catch] = true;
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != CatchTag)
        {
            _Status[Catch] = false;
        }
    }
    //--------------------------------------------------------------------- CAMERA
    // camera moving
    //---------------------------------------------------------------------
    private void CAMERA ()
    {
        _View_Camera.transform.position = this.transform.position + new Vector3(0, 1.0f, 2.0f);
    }
    //--------------------------------------------------------------------- DIRECTION_LIGHT
    // Direction of light
    //---------------------------------------------------------------------
    private void DIRECTION_LIGHT ()
    {
        Vector3 pos = _Light.position - this.transform.position;
        _MeshRenderer.material.SetVector("_LightDir", pos);
    }
    //--------------------------------------------------------------------- GRAVITY
    // gravity for fall of slime
    //---------------------------------------------------------------------
    private void GRAVITY ()
    {
        if(CheckGrounded())
        {
            if(_MoveDirection.y < -0.1f)
            {
                _MoveDirection.y = -0.1f;
            }
        }
        _MoveDirection.y -= 0.1f;
        _Ctrl.Move(_MoveDirection * Time.deltaTime);
    }
    //--------------------------------------------------------------------- isGrounded
    // whether it is grounded
    //---------------------------------------------------------------------
    private bool CheckGrounded()
    {
        if (_Ctrl.isGrounded){
            return true;
        }
        Ray ray = new Ray(this.transform.position + Vector3.up * 0.1f, Vector3.down);
        float range = 0.11f;
        return Physics.Raycast(ray, range);
    }
    //--------------------------------------------------------------------- MOVE
    // for slime moving
    //---------------------------------------------------------------------
    private void MOVE ()
    {
        SNEAK();
        float speed = _Animator.GetFloat(SpeedParameter);
        //------------------------------------------------------------ Speed
        if(Input.GetKey(KeyCode.Z))
        {
            if(speed <= 2){
                speed += 0.01f;
            }
            else if(speed >= 2){
                speed = 2;
            }
        }
        else {
            if(speed >= 1){
                speed -= 0.01f;
            }
            else if(speed <= 1){
                speed = 1;
            }
        }
        _Animator.SetFloat(SpeedParameter, speed);

        //------------------------------------------------------------ Forward
        if (Input.GetKey(KeyCode.UpArrow))
        {
        // velocity
            if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == MoveState)
            {
                Vector3 velocity = this.transform.rotation * new Vector3(0, 0, speed);
                MOVE_XZ(velocity);
                MOVE_RESET();
            }
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag){
                _Animator.CrossFade(MoveState, 0.1f, 0, 0);
            }
        }

        //------------------------------------------------------------ character rotation
        if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow)){
            this.transform.Rotate(Vector3.up, 1.0f);
        }
        else if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow)){
            this.transform.Rotate(Vector3.up, -1.0f);
        }
        if (!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
        {
            if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow)){
                    _Animator.CrossFade(MoveState, 0.1f, 0, 0);
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow)){
                    _Animator.CrossFade(MoveState, 0.1f, 0, 0);
                }
            }
            // rotate stop
            else if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftArrow))
            {
                if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag){
                    _Animator.CrossFade(IdleState, 0.1f, 0, 0);
                }
            }
        }
        KEY_UP();
    }
    //--------------------------------------------------------------------- KEY_UP
    // whether arrow key is key up
    //---------------------------------------------------------------------
    private void KEY_UP ()
    {
        if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != JumpState
            && !_Animator.IsInTransition(0))
        {
            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                if(!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
                {
                  _Animator.CrossFade(IdleState, 0.1f, 0, 0);
                }
            }
            else if (!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
            {
                if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow))
                {
                    if(Input.GetKey(KeyCode.LeftArrow)){
                        _Animator.CrossFade(MoveState, 0.1f, 0, 0);
                    }
                    else if(Input.GetKey(KeyCode.RightArrow)){
                        _Animator.CrossFade(MoveState, 0.1f, 0, 0);
                    }
                    else{
                        _Animator.CrossFade(IdleState, 0.1f, 0, 0);
                    }
                }
            }
        }
    }
    //--------------------------------------------------------------------- MOVE_SUB
    // value for moving
    //---------------------------------------------------------------------
    private void MOVE_XZ (Vector3 velocity)
    {
        _MoveDirection = new Vector3 (velocity.x, _MoveDirection.y, velocity.z);
        _Ctrl.Move(_MoveDirection * Time.deltaTime);
    }
    private void MOVE_RESET()
    {
        _MoveDirection.x = 0;
        _MoveDirection.z = 0;
    }
    //--------------------------------------------------------------------- JUMP
    // for jumping
    //---------------------------------------------------------------------
    private void JUMP ()
    {
        if(CheckGrounded())
        {
            if(Input.GetKeyDown(KeyCode.S)
                && _Animator.GetCurrentAnimatorStateInfo(0).tagHash != JumpTag
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(JumpState, 0.1f, 0, 0);
                // jump power
                _MoveDirection.y = 8.0f;
                _Animator.SetFloat(JumpPoseParameter, _MoveDirection.y);
            }
            if (_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == JumpState
                && !_Animator.IsInTransition(0)
                && JumpPoseParameter < 0)
            {
                _Animator.CrossFade(LandingState, 0.1f, 0, 0);
            }
            if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
                && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == LandingState
                && !_Animator.IsInTransition(0))
            {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)
                    || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                {
                    _Animator.CrossFade(MoveState, 0.1f, 0, 0);
                }
                else{
                    _Animator.CrossFade(IdleState, 0.1f, 0, 0);
                }
            }
        }
        else if(!CheckGrounded())
        {
            if (_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == JumpState
                && !_Animator.IsInTransition(0))
            {
                _Animator.SetFloat(JumpPoseParameter, _MoveDirection.y);
            }
            if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != JumpState
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(JumpState, 0.1f, 0, 0);
            }
        }
    }
    //--------------------------------------------------------------------- DAMAGE
    // play animation of damage
    //---------------------------------------------------------------------
    private void DAMAGE ()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _Animator.CrossFade(DamageState, 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).tagHash == DamageTag
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade(IdleState, 0.3f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- SNEAK
    // switching to a sneak animation
    //---------------------------------------------------------------------
    private void SNEAK ()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            _Animator.SetFloat(SneakParameter, 1);
        }
        else if (Input.GetKeyUp(KeyCode.X))
        {
            _Animator.SetFloat(SneakParameter, 0);
        }
    }
    //--------------------------------------------------------------------- STUN
    // Play a stun animation
    //---------------------------------------------------------------------
    private void STUN ()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            _Animator.CrossFade(StunState, 0.1f, 0, 0);
            if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                _Animator.SetFloat(StunParameter, 0);
            }
            else if(Input.GetKey(KeyCode.LeftShift))
            {
                _Animator.SetFloat(StunParameter, 1);
            }
            else if(Input.GetKey(KeyCode.RightShift))
            {
                _Animator.SetFloat(StunParameter, 2);
            }
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            _Animator.CrossFade(IdleState, 0.1f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- TELEKINESIS
    // Play a telekinesis animation
    //---------------------------------------------------------------------
    private void TELEKINESIS ()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                _Animator.CrossFade(Telekinesis1State, 0.1f, 0, 0);
            }
            else if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _Animator.CrossFade(Telekinesis2State, 0.1f, 0, 0);
            }
        }
        else if (Input.GetKeyUp(KeyCode.T))
        {
            _Animator.CrossFade(IdleState, 0.1f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- LR
    // Switch the LR Parameter
    //---------------------------------------------------------------------
    private void LR ()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _Animator.SetFloat(LRParameter,0);
        }
        else if (Input.GetKeyDown(KeyCode.RightShift))
        {
            _Animator.SetFloat(LRParameter,1);
        }
    }
    //--------------------------------------------------------------------- LOOKBACK
    // Play a lookback animation
    //---------------------------------------------------------------------
    private void LOOKBACK ()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _Animator.CrossFade(LookbackState, 0.5f, 0, 0);
            }
        }
        else if (Input.GetKeyUp(KeyCode.L))
        {
            _Animator.CrossFade(IdleState, 0.5f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- PEEK
    // Play a peek animation
    //---------------------------------------------------------------------
    private void PEEK ()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _Animator.CrossFade(PeekState, 0.3f, 0, 0);
            }
        }
        else if (Input.GetKeyUp(KeyCode.P))
        {
            _Animator.CrossFade(IdleState, 0.3f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- ATTACK
    // Play attack animations
    //---------------------------------------------------------------------
    private void ATTACK ()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _Animator.CrossFade(AttackChargeState, 0.1f, 0, 0);
            }
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == AttackChargeState
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade(AttackState, 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == AttackState
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade(AttackBackState, 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == AttackBackState
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade(IdleState, 0.1f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- CATCH
    // Play catch animations
    //---------------------------------------------------------------------
    private void CATCH ()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if(_Animator.GetFloat(CatchParameter) == 0)
            {
                if(_WithinRange)
                {
                    Vector3 target = _Target.transform.position;
                    target.y = this.transform.position.y;
                    this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
                }

                if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    _Animator.SetFloat(CatchParameter, 1);
                }
                else if(!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    _Animator.SetFloat(CatchParameter, 2);
                    if(_WithinRange)
                    {
                        Vector3 player = this.transform.position;
                        player.y = _Target.transform.position.y;
                        _Target.transform.rotation = Quaternion.LookRotation(player - _Target.transform.position);
                        _TargetAnimator.CrossFade(FearState, 0.1f, 0, 0);
                    }
                }
                _Animator.CrossFade(CatchChargeState, 0.1f, 0, 0);
            }
            else if(_Animator.GetFloat(CatchParameter) > 0
                    && _Animator.GetCurrentAnimatorStateInfo(0).tagHash != CatchTag) 
            {
                _Animator.CrossFade(CatchLetgoState, 0.1f, 0, 0);
            }
        }
        // when enemy got kids in range of catching
        if(_WithinRange)
        {
            if (_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchChargeState
                && !_Animator.IsInTransition(0))
            {
                if(_Animator.GetFloat(CatchParameter) == 1)
                {
                    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f)
                    {
                        _Animator.CrossFade(CatchSuccessState, 0.1f, 0, 0);
                    }
                }
                else if(_Animator.GetFloat(CatchParameter) == 2)
                {
                    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f)
                    {
                        _Animator.CrossFade(CatchSuccessState, 0.1f, 0, 0);
                    }
                }
            }
            if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
                && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchSuccessState
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(CatchSuccessBackState, 0.1f, 0, 0);
                _TargetAnimator.CrossFade(CaughtState, 0.1f, 0, 0);
                _TargetAnimator.SetFloat(CaughtPoseParameter, _Animator.GetFloat(CatchParameter));

                if(_Animator.GetFloat(CatchParameter) == 1)
                {
                    if(_Animator.GetFloat(LRParameter) == 0)
                    {
                        _Target.transform.SetParent(_Hand_L.transform);
                        _Target.transform.position = _Hand_L.transform.position;
                        _Target.transform.rotation = _Hand_L.transform.rotation;
                    }
                    else if(_Animator.GetFloat(LRParameter) == 1)
                    {
                        _Target.transform.SetParent(_Hand_R.transform);
                        _Target.transform.position = _Hand_R.transform.position;
                        _Target.transform.rotation = _Hand_R.transform.rotation;
                    }
                }
                else if(_Animator.GetFloat(CatchParameter) == 2)
                {
                    _Target.transform.SetParent(_Hand_L.transform);
                    _Target.transform.position = _Hand_L.transform.position;
                    _Target.transform.localPosition = new Vector3(-0.35f, 0.06f, 0.16f);
                    _Target.transform.rotation = _Hand_L.transform.rotation * Quaternion.Euler(180,0,270);
                }
            }
        }
        // when enemy didn't get kids in range of catching
        else if(!_WithinRange)
        {
            if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
                && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchChargeState
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(CatchMissState, 0.1f, 0, 0);
            }
            if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
                && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchMissState
                && !_Animator.IsInTransition(0))
            {
                _Animator.CrossFade(CatchMissBackState, 0.1f, 0, 0);
            }
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && !_Animator.IsInTransition(0))
        {
            if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchSuccessBackState)
            {
                _Animator.CrossFade(IdleState, 0.1f, 0, 0);
            }
            else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchMissBackState)
            {
                _Animator.SetFloat(CatchParameter, 0);
                _Animator.CrossFade(IdleState, 0.1f, 0, 0);
            }
            else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchLetgoState)
            {
                _Animator.SetFloat(CatchParameter, 0);
                _Animator.CrossFade(IdleState, 0.1f, 0, 0);
            }
        }
        // let go kids
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f
            && !_Animator.IsInTransition(0)
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CatchLetgoState)
        {
            try
            {
                if(_Target.transform.parent.gameObject != null)
                {
                    _Target.transform.SetParent(null);
                    _Target.transform.localPosition = Vector3.zero;
                    Vector3 velocity = Vector3.zero;
                    
                    if(_Animator.GetFloat(CatchParameter) == 1)
                    {
                        velocity = this.transform.rotation * new Vector3(0, 0, 1.2f);
                        _Target.transform.rotation = this.transform.rotation;
                        _TargetAnimator.CrossFade(CaughtDownState, 0.1f, 0, 0);
                    }
                    else if(_Animator.GetFloat(CatchParameter) == 2)
                    {
                        velocity = this.transform.rotation * new Vector3(0, 0, 0.8f);
                        _Target.transform.rotation = this.transform.rotation * Quaternion.Euler(0,180,0);
                        _TargetAnimator.CrossFade(CowardState, 0.1f, 0, 0);
                    }
                    velocity = new Vector3(velocity.x, 0.03f, velocity.z);
                    _Target.transform.position = this.transform.position + velocity;
                }
            }
            catch (System.NullReferenceException)
            {
                // Nothing method
            }
        }
    }
    //--------------------------------------------------------------------- PUSH
    // Play Push animations
    //---------------------------------------------------------------------
    private void PUSH ()
    {
        if(CheckGrounded() && _WithinRange)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                _Animator.CrossFade(PushState, 0.1f, 0, 0);
                Vector3 target = _Target.transform.position;
                target.y = this.transform.position.y;
                this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);

                _TargetAnimator.CrossFade(PanicState, 0.1f, 0, 0);
                Vector3 player = this.transform.position;
                player.y = _Target.transform.position.y;
                _Target.transform.rotation = Quaternion.LookRotation(player - _Target.transform.position);
            }
            else if (Input.GetKeyUp(KeyCode.Y))
            {
                _Animator.CrossFade(IdleState, 0.1f, 0, 0);
                _TargetAnimator.CrossFade(CowardState, 0.1f, 0, 0);
            }
        }
    }
    //--------------------------------------------------------------------- APPRAISE
    // Play a appraise animation
    //---------------------------------------------------------------------
    private void APPRAISE ()
    {
        if(CheckGrounded() && _WithinRange)
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                _Animator.CrossFade(AppraiseState, 0.3f, 0, 0);
                Vector3 target = _Target.transform.position;
                target.y = this.transform.position.y;
                this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
                Vector3 player = this.transform.position;
                player.y = _Target.transform.position.y;
                _Target.transform.rotation = Quaternion.LookRotation(player - _Target.transform.position);
            }
            else if (Input.GetKeyUp(KeyCode.U))
            {
                _Animator.CrossFade(IdleState, 0.3f, 0, 0);
            }
        }
    }
    //---------------------------------------------------------------------
    // OnTrigger method
    //---------------------------------------------------------------------
    void OnTriggerStay(Collider other)
    {
        if(other.gameObject.name == "BoyNPC2")
        {
            _WithinRange = true;
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.gameObject.name == "BoyNPC2")
        {
            _WithinRange = false;
        }
    }
}
}
