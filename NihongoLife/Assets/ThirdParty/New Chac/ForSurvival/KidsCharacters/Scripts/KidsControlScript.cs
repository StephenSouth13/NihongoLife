using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sample {
public class KidsControlScript : MonoBehaviour
{
  private Animator _Animator;
  private CharacterController _Ctrl;
  private Vector3 _MoveDirection = Vector3.zero;
  private GameObject _View_Camera;
  // Hash
  private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
  private static readonly int MoveState = Animator.StringToHash("Base Layer.move");
  private static readonly int JumpState = Animator.StringToHash("Base Layer.jump");
  private static readonly int LandingState = Animator.StringToHash("Base Layer.landing");
  private static readonly int DiveState = Animator.StringToHash("Base Layer.dive");
  private static readonly int StandUpDiveState = Animator.StringToHash("Base Layer.standup_dive");
  private static readonly int DamageState = Animator.StringToHash("Base Layer.damage");
  private static readonly int DownState = Animator.StringToHash("Base Layer.down");
  private static readonly int FaintState = Animator.StringToHash("Base Layer.faint");
  private static readonly int FaintDamageState = Animator.StringToHash("Base Layer.faint_damage");
  private static readonly int StandUpFaintState = Animator.StringToHash("Base Layer.standup_faint");
  private static readonly int ShiftForwardState = Animator.StringToHash("Base Layer.shift_forward");
  private static readonly int SlidingState = Animator.StringToHash("Base Layer.sliding");
  private static readonly int HangState = Animator.StringToHash("Base Layer.hang");
  private static readonly int ClimbState = Animator.StringToHash("Base Layer.climb");
  private static readonly int StandUpCrouchState = Animator.StringToHash("Base Layer.standup_crouch");
  private static readonly int ShiftCrouchState = Animator.StringToHash("Base Layer.shift_crouch");
  private static readonly int StandUpCrawlState = Animator.StringToHash("Base Layer.standup_crawl");
  private static readonly int ShiftCrawlState = Animator.StringToHash("Base Layer.shift_crawl");
  private static readonly int HappyState = Animator.StringToHash("Base Layer.happy");
  private static readonly int FrustrationState = Animator.StringToHash("Base Layer.frustration");
  private static readonly int KickState = Animator.StringToHash("Base Layer.kick");
  private static readonly int PsychicState = Animator.StringToHash("Base Layer.psychic");
  private static readonly int LookAroundState = Animator.StringToHash("Base Layer.lookaround");
  private static readonly int ListenState = Animator.StringToHash("Base Layer.listen");
  private static readonly int PanicState = Animator.StringToHash("Base Layer.panic");
  private static readonly int ItemGetState = Animator.StringToHash("Base Layer.item_get");
  private static readonly int ItemDropState = Animator.StringToHash("Base Layer.item_drop");
  private static readonly int PocketState = Animator.StringToHash("Base Layer.pocket");
  private static readonly int HammerState = Animator.StringToHash("Base Layer.hammer");
  private static readonly int SmokebombState = Animator.StringToHash("Base Layer.smokebomb");
  private static readonly int SlingshotState = Animator.StringToHash("Base Layer.slingshot");
  private static readonly int SlingshotChargeState = Animator.StringToHash("Base Layer.slingshot_charge");
  private static readonly int ShotState = Animator.StringToHash("Base Layer.shot");
  private static readonly int ShotChargeState = Animator.StringToHash("Base Layer.shot_charge");
  private static readonly int BookState = Animator.StringToHash("Base Layer.book");
  private static readonly int BookReaction1State = Animator.StringToHash("Base Layer.book_reaction1");
  private static readonly int BookReaction2State = Animator.StringToHash("Base Layer.book_reaction2");
  private static readonly int StickyState = Animator.StringToHash("Base Layer.sticky");
  private static readonly int DamageStickyState = Animator.StringToHash("Base Layer.damage_sticky");
  private static readonly int ReleaseStickyState = Animator.StringToHash("Base Layer.release_sticky");
  private static readonly int TrappedState = Animator.StringToHash("Base Layer.trapped");
  private static readonly int DamageTrappedState = Animator.StringToHash("Base Layer.damage_trapped");
  private static readonly int ReleaseTrappedState = Animator.StringToHash("Base Layer.release_trapped");
  private static readonly int TreatOwnState = Animator.StringToHash("Base Layer.treat_own");
  private static readonly int DamageTreatOwnState = Animator.StringToHash("Base Layer.damage_treat_own");
  private static readonly int TreatState = Animator.StringToHash("Base Layer.treat");
  private static readonly int TreatedState = Animator.StringToHash("Base Layer.treated");
  private static readonly int DamageTreatedState = Animator.StringToHash("Base Layer.damage_treated");
  private static readonly int CowardState = Animator.StringToHash("Base Layer.coward");
  private static readonly int FearState = Animator.StringToHash("Base Layer.fear");
  private static readonly int CaughtState = Animator.StringToHash("Base Layer.caught");
  private static readonly int CaughtDownState = Animator.StringToHash("Base Layer.caught_down");
  private static readonly int CaughtStandUpState = Animator.StringToHash("Base Layer.caught_standup");
  private static readonly int KeyState = Animator.StringToHash("Base Layer.key");
  private static readonly int DefaultState = Animator.StringToHash("Base Layer.default");
  private static readonly int ReleaseState = Animator.StringToHash("Base Layer.release");
  private static readonly int CloseState = Animator.StringToHash("Base Layer.close");
  private static readonly int OpenState = Animator.StringToHash("Base Layer.open");
  private static readonly int SwitchState = Animator.StringToHash("Base Layer.switch");
  private static readonly int BoardAttackState = Animator.StringToHash("Base Layer.board_attack");
  private static readonly int BoardBrokenState = Animator.StringToHash("Base Layer.board_broken");
  private static readonly int CabinetOpenState = Animator.StringToHash("Base Layer.cabinet_open");
  private static readonly int CabinetOpenMissState = Animator.StringToHash("Base Layer.cabinet_open_miss");
  private static readonly int CabinetEnterState = Animator.StringToHash("Base Layer.cabinet_enter");
  private static readonly int CabinetCloseState = Animator.StringToHash("Base Layer.cabinet_close");
  private static readonly int CabinetIdleState = Animator.StringToHash("Base Layer.cabinet_idle");
  private static readonly int CabinetAttackState = Animator.StringToHash("Base Layer.cabinet_attack");
  private static readonly int CabinetExit1State = Animator.StringToHash("Base Layer.cabinet_exit1");
  private static readonly int CabinetExit2State = Animator.StringToHash("Base Layer.cabinet_exit2");
  private static readonly int CabinetExitState = Animator.StringToHash("Base Layer.cabinet_exit");
  private static readonly int CabinetExitJumpState = Animator.StringToHash("Base Layer.cabinet_exit_jump");
  private static readonly int CabinetSurprisedState = Animator.StringToHash("Base Layer.cabinet_surprised");
  // tag
  private static readonly int JumpTag = Animator.StringToHash("Jump");
  private static readonly int DiveTag = Animator.StringToHash("Dive");
  private static readonly int DamageTag = Animator.StringToHash("Damage");
  private static readonly int FaintTag = Animator.StringToHash("Faint");
  private static readonly int SlidingTag = Animator.StringToHash("Sliding");
  private static readonly int CaughtTag = Animator.StringToHash("Caught");
  private static readonly int HangTag = Animator.StringToHash("Hang");
  private static readonly int CrouchTag = Animator.StringToHash("Crouch");
  private static readonly int CrawlTag = Animator.StringToHash("Crawl");
  private static readonly int ItemTag = Animator.StringToHash("Item");
  private static readonly int StickyTag = Animator.StringToHash("Sticky");
  private static readonly int TrappedTag = Animator.StringToHash("Trapped");
  private static readonly int TreatOwnTag = Animator.StringToHash("TreatOwn");
  private static readonly int SlingshotTag = Animator.StringToHash("Slingshot");
  private static readonly int BookTag = Animator.StringToHash("Book");
  private static readonly int CabinetTag = Animator.StringToHash("Cabinet");
  // parameter
  private static readonly int SpeedParameter = Animator.StringToHash("Speed");
  private static readonly int JumpPoseParameter = Animator.StringToHash("JumpPose");
  private static readonly int FacingParameter = Animator.StringToHash("Facing");
  private static readonly int PoseParameter = Animator.StringToHash("Pose");
  private static readonly int DamageParameter = Animator.StringToHash("Damage");
  private static readonly int SneakParameter = Animator.StringToHash("Sneak");
  private static readonly int StaminaParameter = Animator.StringToHash("Stamina");
  private static readonly int FlashlightParameter = Animator.StringToHash("Flashlight");
  private static readonly int CaughtParameter = Animator.StringToHash("Caught");
  private static readonly int TriggerSwitchParameter = Animator.StringToHash("TriggerSwitch");
  private static readonly int LRParameter = Animator.StringToHash("LR");
  private static readonly int AnimationSpeedParameter = Animator.StringToHash("AnimationSpeed");

  // GameObject for action
  private GameObject _HillEdge;
  private GameObject _KickObj;
  // Item
  [SerializeField] private GameObject _Hammer;
  [SerializeField] private GameObject _Smokebomb;
  [SerializeField] private GameObject _Slingshot;
  [SerializeField] private GameObject _Book;
  [SerializeField] private GameObject _Flashlight;
  private Animator _SlingshotAnimator;
  private GameObject _ObjHammer;
  private GameObject _ObjSmokebomb;
  private GameObject _ObjSlingshot;
  private GameObject _ObjBook;
  private GameObject _ObjBandage;
  private GameObject _ObjFlashlight;
  private GameObject _EffectSmoke;
  private GameObject _ObjItem;
  // Slider
  private Slider _Slider;
  private float _SliderGauge = 0;
  private Slider _StaminaSlider;
  // Text
  private GameObject _ObjTextInfo;
  private Text _TextInfo;
  // NPC
  private GameObject _BoyNPC;
  private Animator _BoyNPCAnimator;
  private Animator _BoyNPC3Animator;
  // Effect
  private GameObject _EffectFootstep;
  // EventObj
  private SkinnedMeshRenderer _EventObjMaterial;
  private Animator _EventObjAnimator;
  private Collider _EventObjCollider;
  private GameObject _EventObjStone;
  private GameObject _EffectStoneBreak;
  // Trap
  private Animator _TrapAnimator;
  // Switch
  private Animator _SwitchAnimator;
  // Board
  private Animator _BoardAnimator;
  private BoxCollider _BoardCollider;
  // Cabinet
  private Animator _CabinetAnimator;
  private Animator _Cabinet2Animator;
  // for character move
  private Vector3 StartPos;
  private Vector3 EndPos;
  private Quaternion StartRot;
  private Quaternion EndRot;

  // Flag
  private bool _HaveItem = false;
  private bool _HaveHammer = false;
  private bool _HaveSmokebomb = false;
  private bool _HaveSlingshot = false;
  private bool _HaveBook = false;
  private bool _HaveBandage = false;
  private bool _HaveFlashlight = false;

  // Icon
  private GameObject _StatusIcon;
  private Image _StatusIconImage;
  private Sprite _StatusIconStand;
  private Sprite _StatusIconInjure1;
  private Sprite _StatusIconInjure2;
  private Sprite _StatusIconSkull;
  private Sprite _StatusIconSticky;
  private Sprite _StatusIconTrapped;
  private GameObject _BloodIcon;
  private GameObject _Item1Icon;
  private Image _Item1IconImage;
  private Sprite _ItemIconHammer;
  private Sprite _ItemIconSmokebomb;
  private Sprite _ItemIconSlingshot;
  private Sprite _ItemIconBook;
  private Sprite _ItemIconBandage;
  private Sprite _ItemIconFlashlight;
  //----------------------
  // Start
  //----------------------
  void Start()
  {
    _Animator = this.GetComponent<Animator>();
    _Ctrl = this.GetComponent<CharacterController>();
    _View_Camera = GameObject.Find("Main Camera");
    _HillEdge = GameObject.Find("HillEdge");
    _KickObj = GameObject.Find("KickObj");
    _Hammer.gameObject.SetActive(false);
    _Smokebomb.gameObject.SetActive(false);
    _SlingshotAnimator = _Slingshot.GetComponent<Animator>();
    _Slingshot.gameObject.SetActive(false);
    _Book.gameObject.SetActive(false);
    _Flashlight.gameObject.SetActive(false);
    _ObjHammer = GameObject.Find("ObjHammer");
    _ObjSmokebomb = GameObject.Find("ObjSmokebomb");
    _ObjSlingshot = GameObject.Find("ObjSlingshot");
    _ObjBook = GameObject.Find("ObjBook");
    _ObjBandage = GameObject.Find("ObjBandage");
    _ObjFlashlight = GameObject.Find("ObjFlashlight");
    _EffectSmoke = Resources.Load<GameObject>("Prefabs/Effect/EffectSmoke");
    _Slider = GameObject.Find("Canvas/Slider").GetComponent<Slider>();
    _Slider.gameObject.SetActive(false);
    _StaminaSlider = GameObject.Find("Canvas/StaminaSlider").GetComponent<Slider>();
    _ObjTextInfo = GameObject.Find("Canvas/TextInfo");
    _TextInfo = GameObject.Find("Canvas/TextInfo/Text").GetComponent<Text>();
    _BoyNPC = GameObject.Find("BoyNPC");
    _BoyNPCAnimator = _BoyNPC.GetComponent<Animator>();
    _StatusIcon = GameObject.Find("Canvas/Status/Icon");
    _StatusIconImage = _StatusIcon.GetComponent<Image>();
    _StatusIconImage.color = new Color (0.8f, 1, 1);
    _BloodIcon = GameObject.Find("Canvas/Status/IconBlood");
    _BloodIcon.gameObject.SetActive(false);
    _StatusIconStand = Resources.Load<Sprite>("Images/status_stand");
    _StatusIconInjure1 = Resources.Load<Sprite>("Images/status_injure1");
    _StatusIconInjure2 = Resources.Load<Sprite>("Images/status_injure2");
    _StatusIconSkull = Resources.Load<Sprite>("Images/status_skull");
    _StatusIconSticky = Resources.Load<Sprite>("Images/status_sticky");
    _StatusIconTrapped = Resources.Load<Sprite>("Images/status_trapped");
    _Item1Icon = GameObject.Find("Canvas/Item1/Icon");
    _Item1IconImage = _Item1Icon.GetComponent<Image>();
    _Item1Icon.gameObject.SetActive(false);
    _ItemIconHammer = Resources.Load<Sprite>("Images/item_hammer");
    _ItemIconSmokebomb = Resources.Load<Sprite>("Images/item_smokebomb");
    _ItemIconSlingshot = Resources.Load<Sprite>("Images/item_slingshot");
    _ItemIconBook = Resources.Load<Sprite>("Images/item_book");
    _ItemIconBandage = Resources.Load<Sprite>("Images/item_bandage");
    _ItemIconFlashlight = Resources.Load<Sprite>("Images/item_flashlight");
    _EffectFootstep = Resources.Load<GameObject>("Prefabs/Effect/EffectFootstep");
    _EventObjMaterial = GameObject.Find("eventObj/EventObj").GetComponent<SkinnedMeshRenderer>();
    _EventObjAnimator = GameObject.Find("eventObj").GetComponent<Animator>();
    _EventObjCollider = GameObject.Find("eventObj").GetComponent<SphereCollider>();
    _EventObjStone = GameObject.Find("eventObj_stone");
    _EffectStoneBreak = Resources.Load<GameObject>("Prefabs/Effect/EffectStoneBreak");
    _TrapAnimator = GameObject.Find("Trap").GetComponent<Animator>();
    _SwitchAnimator = GameObject.Find("Switch").GetComponent<Animator>();
    _BoardAnimator = GameObject.Find("Board").GetComponent<Animator>();
    _BoardCollider = GameObject.Find("Board").GetComponent<BoxCollider>();
    _CabinetAnimator = GameObject.Find("Cabinet").GetComponent<Animator>();
    _Cabinet2Animator = GameObject.Find("Cabinet2").GetComponent<Animator>();
    _BoyNPC3Animator = GameObject.Find("BoyNPC3").GetComponent<Animator>();
  }

  void Update()
  {
    CAMERA();
    GRAVITY();
    STATUS();
    STAMINA();

    if(!_Status.ContainsValue( true ))
    {
        MOVE();
        JUMP();
        DAMAGE();
        FAINT();
        SLIDING();
        CROUCH();
        CRAWL();
        ETCETERA_ANIMATION();
        ETCETERA_ANIMATION2();
        ITEM_DROP();
        HAMMER();
        SMOKEBOMB();
        SLINGSHOT();
        //BOOK();
        TREAT_OWN();
        FLASHLIGHT();
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
        FAINT();
        DIVE();
      }
      else if(status_name == Dive)
      {
        DIVE();
      }
      else if(status_name == Damage)
      {
        DAMAGE();
      }
      else if(status_name == Faint)
      {
        FAINT();
      }
      else if(status_name == Sliding)
      {
        SLIDING();
      }
      else if(status_name == Hang)
      {
        CLIMB();
      }
      else if(status_name == Crouch)
      {
        CROUCH();
        MOVE();
        DAMAGE();
        ETCETERA_ANIMATION2();
      }
      else if(status_name == Crawl)
      {
        CRAWL();
        MOVE();
        DAMAGE();
      }
      else if(status_name == Kick)
      {
        // nothing method
      }
      else if(status_name == Item)
      {
        ITEM_ACTION();
      }
      else if(status_name == Hammer)
      {
        HAMMER();
      }
      else if(status_name == Smokebomb)
      {
        SMOKEBOMB();
      }
      else if(status_name == Slingshot)
      {
        SLINGSHOT();
      }
      else if(status_name == Book)
      {
        BOOK();
      }
      else if(status_name == Sticky)
      {
        STICKY();
      }
      else if(status_name == Trapped)
      {
        TRAPPED();
      }
      else if(status_name == TreatOwn)
      {
        TREAT_OWN();
      }
      else if(status_name == Treat)
      {
        TREAT();
      }
      else if(status_name == Psychic)
      {
        PSYCHIC();
      }
      else if(status_name == Switch)
      {
        // nothing method
      }
      else if(status_name == Cabinet)
      {
        EXIT_CABINET();
      }
    }
  }
  //--------------------------------------------------------------------- STATUS
  // Flags to control slime's action
  // It is used by method in Update()
  //---------------------------------------------------------------------
  private const int Jump = 1;
  private const int Dive = 2;
  private const int Damage = 3;
  private const int Faint = 4;
  private const int Sliding = 5;
  private const int Caught = 6;
  private const int Hang = 7;
  private const int Crouch = 8;
  private const int Crawl = 9;
  private const int Kick = 10;
  private const int Item = 11;
  private const int Hammer = 12;
  private const int Smokebomb = 13;
  private const int Slingshot = 14;
  private const int Book = 15;
  private const int Sticky = 16;
  private const int Trapped = 17;
  private const int TreatOwn = 18;
  private const int Treat = 19;
  private const int Psychic = 20;
  private const int Switch = 21;
  private const int Cabinet = 22;
  private Dictionary<int, bool> _Status = new Dictionary<int, bool>
  {
      {Jump, false },
      {Dive, false },
      {Damage, false },
      {Faint, false },
      {Sliding, false },
      {Caught, false},
      {Hang, false},
      {Crouch, false},
      {Crawl, false},
      {Kick, false},
      {Item, false},
      {Hammer, false},
      {Smokebomb, false},
      {Slingshot, false},
      {Book, false},
      {Sticky, false},
      {Trapped, false},
      {TreatOwn, false},
      {Treat, false},
      {Psychic, false},
      {Switch, false},
      {Cabinet, false},
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

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == DiveTag)
      {
          _Status[Dive] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != DiveTag)
      {
          _Status[Dive] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == DamageTag)
      {
          _Status[Damage] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != DamageTag)
      {
          _Status[Damage] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == FaintTag)
      {
          _Status[Faint] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != FaintTag)
      {
          _Status[Faint] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == SlidingTag)
      {
          _Status[Sliding] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != SlidingTag)
      {
          _Status[Sliding] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == CaughtTag)
      {
          _Status[Caught] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != CaughtTag)
      {
          _Status[Caught] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == HangTag)
      {
          _Status[Hang] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != HangTag)
      {
          _Status[Hang] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == CrouchTag)
      {
          _Status[Crouch] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != CrouchTag)
      {
          _Status[Crouch] = false;
      }

      if(_Animator.GetFloat(PoseParameter) > 0 && _Animator.GetFloat(PoseParameter) <= 1)
      {
          _Status[Crouch] = true;
      }
      else if(_Animator.GetFloat(PoseParameter) == 0 && _Animator.GetFloat(PoseParameter) > 1)
      {
          _Status[Crouch] = false;
      }

      if(_Animator.GetFloat(PoseParameter) > 1)
      {
        _Status[Crawl] = true;
      }
      else if(_Animator.GetFloat(PoseParameter) <= 1)
      {
        _Status[Crawl] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == KickState)
      {
          _Status[Kick] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != KickState)
      {
          _Status[Kick] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == ItemTag)
      {
          _Status[Item] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != ItemTag)
      {
          _Status[Item] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HammerState)
      {
          _Status[Hammer] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != HammerState)
      {
          _Status[Hammer] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == SmokebombState)
      {
          _Status[Smokebomb] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != SmokebombState)
      {
          _Status[Smokebomb] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == SlingshotTag)
      {
          _Status[Slingshot] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != SlingshotTag)
      {
          _Status[Slingshot] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == BookTag)
      {
          _Status[Book] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != BookTag)
      {
          _Status[Book] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == StickyTag)
      {
          _Status[Sticky] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != StickyTag)
      {
          _Status[Sticky] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == TrappedTag)
      {
          _Status[Trapped] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != TrappedTag)
      {
          _Status[Trapped] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == TreatOwnTag)
      {
          _Status[TreatOwn] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != TreatOwnTag)
      {
          _Status[TreatOwn] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == TreatState)
      {
          _Status[Treat] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != TreatState)
      {
          _Status[Treat] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == PsychicState)
      {
          _Status[Psychic] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != PsychicState)
      {
          _Status[Psychic] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == SwitchState)
      {
          _Status[Switch] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != SwitchState)
      {
          _Status[Switch] = false;
      }

      if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == CabinetTag)
      {
          _Status[Cabinet] = true;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash != CabinetTag)
      {
          _Status[Cabinet] = false;
      }
  }
  //--------------------------------------------------------------------- CAMERA
  // camera moving
  //---------------------------------------------------------------------
  private void CAMERA ()
  {
    _View_Camera.transform.position = this.transform.position + new Vector3(0, 0.5f, 2.0f);
  }
  //--------------------------------------------------------------------- GRAVITY
  // gravity for fall of slime
  //---------------------------------------------------------------------
  private void GRAVITY ()
  {
    if(_Ctrl.enabled)
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
  }
  //--------------------------------------------------------------------- isGrounded
  // whether it is grounded
  //---------------------------------------------------------------------
  private bool CheckGrounded()
  {
    if (_Ctrl.isGrounded && _Ctrl.enabled)
    {
      return true;
    }
    Ray ray = new Ray(this.transform.position + Vector3.up * 0.1f, Vector3.down);
    float range = 0.2f;
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
        Vector3 velocity = this.transform.rotation * new Vector3(0, 0, speed * _Animator.GetFloat(StaminaParameter));
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
        _MoveDirection.y = 5.0f;
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
  //--------------------------------------------------------------------- DIVE
  // play animation of dive
  //---------------------------------------------------------------------
  private void DIVE ()
  {
    if(!CheckGrounded())
    {
      if(Input.GetKeyDown(KeyCode.S)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == JumpState
        && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(DiveState, 0.1f, 0, 0);
        _MoveDirection.y += 3.0f;
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DiveState)
      {
        Vector3 velocity = this.transform.rotation * new Vector3(0, 0, 5);
        MOVE_XZ(velocity);
        MOVE_RESET();
      }
    }
    else if(CheckGrounded())
    {
      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DiveState
          && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(StandUpDiveState, 0.1f, 0, 0);
      }
      else if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
              && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == StandUpDiveState
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
  }
  //--------------------------------------------------------------------- DAMAGE
  // play animation of damage
  //---------------------------------------------------------------------
  private void DAMAGE ()
  {
    if (Input.GetKeyDown(KeyCode.Q))
    {
      FACING();
      float damage = _Animator.GetFloat(DamageParameter) + 1;
      DAMAGE_LV(damage);
      if (_Animator.GetFloat(DamageParameter) < 2 && _Animator.GetFloat(PoseParameter) < 2)
      {
        _Animator.CrossFade(DamageState, 0.1f, 0, 0);
      }
      else if (_Animator.GetFloat(DamageParameter) == 2 && _Animator.GetFloat(PoseParameter) < 2)
      {
        _Animator.CrossFade(DownState, 0.1f, 0, 0);
      }
      else if (_Animator.GetFloat(DamageParameter) == 3 || _Animator.GetFloat(PoseParameter) == 2)
      {
        _Animator.CrossFade(FaintDamageState, 0.1f, 0, 0);
      }
    }
    if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash == DamageTag
        && !_Animator.IsInTransition(0))
    {
      if (_Animator.GetFloat(DamageParameter) < 3)
      {
        _Animator.CrossFade(IdleState, 0.3f, 0, 0);
      }
      else if (_Animator.GetFloat(DamageParameter) == 3)
      {
        _Animator.CrossFade(FaintState, 0.1f, 0, 0);
      }
    }
  }
  private void DAMAGE_LV (float damage_lv)
  {
    _Animator.SetFloat(DamageParameter, damage_lv);
    if(damage_lv == 0)
    {
      _StatusIconImage.sprite = _StatusIconStand;
      _StatusIconImage.color = new Color (0.8f, 1, 1);
      _BloodIcon.gameObject.SetActive(false);
    }
    else if(damage_lv == 1)
    {
      _StatusIconImage.sprite = _StatusIconInjure1;
      _StatusIconImage.color = new Color (0.9f, 0.9f, 0.1f);
      _BloodIcon.gameObject.SetActive(true);
    }
    else if(damage_lv == 2)
    {
      _StatusIconImage.sprite = _StatusIconInjure2;
      _StatusIconImage.color = new Color (0.8f, 0.1f, 0.1f);
      _BloodIcon.gameObject.SetActive(true);
    }
    else if(damage_lv == 3)
    {
      _StatusIconImage.sprite = _StatusIconSkull;
      _StatusIconImage.color = new Color (0.5f, 0.5f, 0.5f);
      _BloodIcon.gameObject.SetActive(false);
    }
  }
  //--------------------------------------------------------------------- FAINT
  // play animation of down and jump of resurrection
  //---------------------------------------------------------------------
  private void FAINT ()
  {
    if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DownState
        && !_Animator.IsInTransition(0))
    {
      if(_Animator.GetFloat(DamageParameter) == 2)
      {
        if(_Animator.GetFloat(FacingParameter) == 0)
        {
          _Animator.CrossFade(ShiftForwardState, 0.3f, 0, 0);
        }
        else if(_Animator.GetFloat(FacingParameter) == 1)
        {
          _Animator.CrossFade(IdleState, 0.3f, 0, 0);
        }
        _Animator.SetFloat(PoseParameter, 2);
      }
    }
    if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ShiftForwardState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.3f, 0, 0);
    }

    if (Input.GetKeyDown(KeyCode.E)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == FaintState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(StandUpFaintState, 0.1f, 0, 0);
      _Animator.SetFloat(PoseParameter, 0);
      DAMAGE_LV(0);
    }
  }
  //--------------------------------------------------------------------- FACING
  // value of animator parameters for animation facing
  //---------------------------------------------------------------------
  private void FACING ()
  {
    if(Input.GetKey(KeyCode.DownArrow))
    {
      _Animator.SetFloat(FacingParameter, 0);
    }
    else
    {
      _Animator.SetFloat(FacingParameter, 1);
    }
  }
  //--------------------------------------------------------------------- SLIDING
  // play animation of sliding
  //---------------------------------------------------------------------
  private void SLIDING ()
  {
    if(Input.GetKey(KeyCode.D))
    {
      _Animator.CrossFade(SlidingState, 0.1f, 0, 0);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).tagHash == SlidingTag)
    {
      Vector3 velocity = this.transform.rotation * new Vector3(0, 0, 5);
      MOVE_XZ(velocity);
      MOVE_RESET();
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash == SlidingTag
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
  //--------------------------------------------------------------------- HANG
  // method so that the character climb a cliff
  //---------------------------------------------------------------------
  private void CLIMB ()
  {
    if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)
        || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
    {
      if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f
          && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HangState
          && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(ClimbState, 0.1f, 0, 0);
      }
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ClimbState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
    }
  }
  //--------------------------------------------------------------------- CROUCH
  // play animation of crouch
  //---------------------------------------------------------------------
  private void CROUCH ()
  {
    if (Input.GetKeyDown(KeyCode.X))
    {
      _Animator.CrossFade(ShiftCrouchState, 0.1f, 0, 0);
      //_Animator.SetFloat(PoseParameter, 0.1f);
    }
    else if (Input.GetKeyUp(KeyCode.X))
    {
      _Animator.CrossFade(StandUpCrouchState, 0.1f, 0, 0);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ShiftCrouchState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _Animator.SetFloat(PoseParameter, 1);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == StandUpCrouchState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _Animator.SetFloat(PoseParameter, 0);
    }
  }
  //--------------------------------------------------------------------- CRAWL
  // play animation of crawl
  //---------------------------------------------------------------------
  private void CRAWL ()
  {
    if (Input.GetKeyDown(KeyCode.C)  && _Animator.GetFloat(DamageParameter) < 2)
    {
      _Animator.CrossFade(ShiftCrawlState, 0.1f, 0, 0);
      //_Animator.SetFloat(PoseParameter, 1.1f);
    }
    else if (Input.GetKeyUp(KeyCode.C) && _Animator.GetFloat(DamageParameter) < 2)
    {
      _Animator.CrossFade(StandUpCrawlState, 0.1f, 0, 0);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ShiftCrawlState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _Animator.SetFloat(PoseParameter, 2);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == StandUpCrawlState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _Animator.SetFloat(PoseParameter, 0);
    }
  }
  //--------------------------------------------------------------------- SNEAK
  // switching to a sneak animation
  //---------------------------------------------------------------------
  private void SNEAK ()
  {
    if (Input.GetKeyDown(KeyCode.A))
    {
      _Animator.SetFloat(SneakParameter, 1);
    }
    else if (Input.GetKeyUp(KeyCode.A))
    {
      _Animator.SetFloat(SneakParameter, 0);
    }
  }
  //--------------------------------------------------------------------- STAMINA
  // switching to a tired animation
  //---------------------------------------------------------------------
  private void STAMINA ()
  {
    float stamina = _Animator.GetFloat(StaminaParameter);
    if(_Animator.GetFloat(SpeedParameter) == 2
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == MoveState)
    {
      stamina -= 0.2f * Time.deltaTime;
      if(stamina < 0)
      {
        stamina = 0;
      }
    }
    else if(_Animator.GetFloat(SpeedParameter) == 1
            || _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != MoveState)
    {
      stamina += 0.5f * Time.deltaTime;
      if(stamina > 1)
      {
        stamina = 1;
      }
    }
    _Animator.SetFloat(StaminaParameter, stamina);
    _StaminaSlider.value = _Animator.GetFloat(StaminaParameter);
  }
  //--------------------------------------------------------------------- ETCETERA_ANIMATION
  // play animations of etcetera
  //---------------------------------------------------------------------
  private void ETCETERA_ANIMATION ()
  {
    if (Input.GetKeyDown(KeyCode.Y) && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(HappyState, 0.1f, 0, 0);
    }
    else if (Input.GetKeyDown(KeyCode.U) && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(FrustrationState, 0.1f, 0, 0);
    }
  }
  private void ETCETERA_ANIMATION2 ()
  {
    if (Input.GetKeyDown(KeyCode.I) && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(LookAroundState, 0.1f, 0, 0);
    }
    else if (Input.GetKeyDown(KeyCode.O) && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(ListenState, 0.1f, 0, 0);
    }
    else if (Input.GetKeyDown(KeyCode.P) && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(PanicState, 0.1f, 0, 0);
    }
  }
  //--------------------------------------------------------------------- HAMMER
  // play animations of hammer
  //---------------------------------------------------------------------
  private void HAMMER ()
  {
    if(CheckGrounded() && _HaveItem && _HaveHammer)
    {
      if (Input.GetKeyDown(KeyCode.F) && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(HammerState, 0.0f, 0, 0);
        _Hammer.gameObject.SetActive(true);
      }
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 2.5f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == HammerState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.5f, 0, 0);
      _Hammer.gameObject.SetActive(false);
    }
  }
  //--------------------------------------------------------------------- SMOKEBOMB
  // play animations of smokebomb
  //---------------------------------------------------------------------
  private void SMOKEBOMB ()
  {
    if(CheckGrounded() && _HaveItem && _HaveSmokebomb)
    {
      if (Input.GetKeyDown(KeyCode.F) && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(SmokebombState, 0.0f, 0, 0);
        _Smokebomb.gameObject.SetActive(true);
      }
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == SmokebombState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
    }
  }
  //--------------------------------------------------------------------- SLINGSHOT
  // play animations of slingshot
  //---------------------------------------------------------------------
  private void SLINGSHOT ()
  {
    if(CheckGrounded() && _HaveItem && _HaveSlingshot)
    {
      if (Input.GetKeyDown(KeyCode.F) && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(SlingshotChargeState, 0.0f, 0, 0);
        _Slingshot.gameObject.SetActive(true);
        _SlingshotAnimator.CrossFade(ShotChargeState, 0.0f, 0, 0);
      }
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == SlingshotChargeState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(SlingshotState, 0.1f, 0, 0);
      _SlingshotAnimator.CrossFade(ShotState, 0.1f, 0, 0);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == SlingshotState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _SlingshotAnimator.CrossFade(IdleState, 0.1f, 0, 0);
      _Slingshot.gameObject.SetActive(false);
    }
  }
  //--------------------------------------------------------------------- BOOK
  // play animations of book
  //---------------------------------------------------------------------
  private void BOOK ()
  {
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && !_Animator.IsInTransition(0))
    {
      if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == BookReaction1State
          || _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == BookReaction2State)
      {
        _Animator.CrossFade(IdleState, 0.5f, 0, 0);
        _Book.gameObject.SetActive(false);
      }
    }
  }
  //--------------------------------------------------------------------- FLASHLIGHT
  // behavior when that's having a flashlight
  //---------------------------------------------------------------------
  private void FLASHLIGHT ()
  {
    if(_HaveFlashlight)
    {
      if(Input.GetKeyDown(KeyCode.F))
      {
        _Animator.SetFloat(FlashlightParameter, 1);
        _Flashlight.gameObject.SetActive(true);
      }
      else if(Input.GetKeyUp(KeyCode.F))
      {
        _Animator.SetFloat(FlashlightParameter, 0);
        _Flashlight.gameObject.SetActive(false);
      }
    }
  }
  //--------------------------------------------------------------------- ITEM
  // play animations of item get and drop
  //---------------------------------------------------------------------
  private void ITEM_ACTION ()
  {
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.7f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ItemGetState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(PocketState, 0.3f, 0, 0);
      _HaveItem = true;
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == PocketState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
    }
    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ItemDropState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _HaveItem = false;
      _HaveHammer = false;
      _HaveSmokebomb = false;
      _HaveSlingshot = false;
      _HaveBook = false;
      _HaveBandage = false;
      _HaveFlashlight = false;
      _ObjItem.gameObject.SetActive(true);
      _ObjItem.transform.position = this.transform.position + this.transform.rotation * new Vector3(0, 0.25f, 0.5f);
      _Item1Icon.gameObject.SetActive(false);
    }
  }
  private void ITEM_DROP ()
  {
    if(CheckGrounded())
    {
      if (Input.GetKeyDown(KeyCode.W) && _HaveItem
          && _Animator.GetCurrentAnimatorStateInfo(0).tagHash != ItemTag
          && !_Animator.IsInTransition(0))
      {
        _Animator.CrossFade(ItemDropState, 0.1f, 0, 0);
        _Animator.SetFloat(FlashlightParameter, 0);
        _Flashlight.gameObject.SetActive(false);
      }
    }
  }
  //--------------------------------------------------------------------- STICKY
  // escape from sticky panel
  //---------------------------------------------------------------------
  private void STICKY ()
  {
    if (Input.GetKeyDown(KeyCode.R)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == StickyState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(ReleaseStickyState, 0.1f, 0, 0);
      _Slider.gameObject.SetActive(true);
      _SliderGauge = 0;
    }
    if (Input.GetKeyUp(KeyCode.R)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ReleaseStickyState
        && !_Animator.IsInTransition(0))
    {
      if(_SliderGauge >= 1.0f)
      {
        _Animator.CrossFade(IdleState, 0.1f, 0, 0);
        DAMAGE_LV(_Animator.GetFloat(DamageParameter));
      }
      else if(_SliderGauge < 1.0f)
      {
        _Animator.CrossFade(StickyState, 0.1f, 0, 0);
      }
      _Slider.gameObject.SetActive(false);
      _TextInfo.text = "";
    }
    if (Input.GetKey(KeyCode.R))
    {
      _SliderGauge += 0.5f * Time.deltaTime;
      _Slider.value = _SliderGauge;
    }
  }
  //--------------------------------------------------------------------- TRAPPED
  // escape from trap
  //---------------------------------------------------------------------
  private void TRAPPED ()
  {
    if (Input.GetKeyDown(KeyCode.R)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == TrappedState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(ReleaseTrappedState, 0.1f, 0, 0);
      _TrapAnimator.CrossFade(ReleaseState, 0.1f, 0, 0);
      _Slider.gameObject.SetActive(true);
      _SliderGauge = 0;
    }
    if (Input.GetKeyUp(KeyCode.R)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == ReleaseTrappedState
        && !_Animator.IsInTransition(0))
    {
      if(_SliderGauge >= 1.0f)
      {
        _Animator.CrossFade(IdleState, 0.1f, 0, 0);
        _TrapAnimator.CrossFade(DefaultState, 0.1f, 0, 0);
        DAMAGE_LV(_Animator.GetFloat(DamageParameter));
      }
      else if(_SliderGauge < 1.0f)
      {
        _Animator.CrossFade(TrappedState, 0.1f, 0, 0);
        _TrapAnimator.CrossFade(CloseState, 0.1f, 0, 0);
      }
      _Slider.gameObject.SetActive(false);
      _TextInfo.text = "";
    }
    if (Input.GetKey(KeyCode.R))
    {
      _SliderGauge += 0.5f * Time.deltaTime;
      _Slider.value = _SliderGauge;
    }
  }
  //--------------------------------------------------------------------- TREAT
  // play animation of treat
  //---------------------------------------------------------------------
  // treat own
  private void TREAT_OWN ()
  {
    if (Input.GetKeyDown(KeyCode.F)
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash != TreatOwnTag
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != DamageTreatOwnState
        && !_Animator.IsInTransition(0)
        && _Animator.GetFloat(DamageParameter) >= 1
        && _HaveBandage)
    {
      _Animator.CrossFade(TreatOwnState, 0.1f, 0, 0);
      _Slider.gameObject.SetActive(true);
      _SliderGauge = 0;
    }
    if (Input.GetKeyUp(KeyCode.F)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == TreatOwnState
        && !_Animator.IsInTransition(0)
        && _Animator.GetFloat(DamageParameter) <= 1
        && _HaveBandage)
    {
      if(_SliderGauge >= 1.0f)
      {
        float damage = _Animator.GetFloat(DamageParameter) - 1;
        DAMAGE_LV(damage);
        _Animator.CrossFade(IdleState, 0.1f, 0, 0);
        _HaveItem = false;
        _HaveBandage = false;
        _ObjBandage.gameObject.SetActive(true);
        _ObjBandage.transform.position = new Vector3(-1, 0.25f, -2);
        _Item1Icon.gameObject.SetActive(false);
      }
      else if(_SliderGauge < 1.0f)
      {
        _Animator.CrossFade(DamageTreatOwnState, 0.1f, 0, 0);
      }
      _Slider.gameObject.SetActive(false);
    }
    if (Input.GetKey(KeyCode.F) && _HaveBandage)
    {
      _SliderGauge += 0.5f * Time.deltaTime;
      _Slider.value = _SliderGauge;
    }

    if(_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == DamageTreatOwnState
        && !_Animator.IsInTransition(0))
    {
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
    }
  }
  // treat
  private void TREAT ()
  {
    if (Input.GetKeyUp(KeyCode.G)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == TreatState
        && !_Animator.IsInTransition(0))
    {
      if(_SliderGauge >= 1.0f)
      {
        _BoyNPCAnimator.CrossFade(IdleState, 0.1f, 0, 0);
      }
      else if(_SliderGauge < 1.0f)
      {
        _BoyNPCAnimator.CrossFade(DamageTreatedState, 0.1f, 0, 0);
      }
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _Slider.gameObject.SetActive(false);
    }
    if (Input.GetKey(KeyCode.G))
    {
      _SliderGauge += 0.5f * Time.deltaTime;
      _Slider.value = _SliderGauge;
    }
  }
  //--------------------------------------------------------------------- PSYCHIC
  // play animations of psychic & caught
  //---------------------------------------------------------------------
  private void PSYCHIC ()
  {
    if (Input.GetKeyUp(KeyCode.H)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == PsychicState
        && !_Animator.IsInTransition(0))
    {
      _BoyNPCAnimator.CrossFade(CaughtDownState, 0.1f, 0, 0);
      _Animator.CrossFade(IdleState, 0.1f, 0, 0);
      _BoyNPC.transform.position = _BoyNPC.transform.position + new Vector3(0,-1,0);
    }
  }
  //---------------------------------------------------------------------
  // Remap a value range to another value range
  //---------------------------------------------------------------------
  private float REMAP_VALUE(float value, float start1, float stop1, float start2, float stop2)
  {
    return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
  }
  //---------------------------------------------------------------------
  // Move the character when the character enters the cabinet.
  //---------------------------------------------------------------------
  // Coroutine
  private IEnumerator CharacterMove (Vector3 start_pos, Vector3 end_pos, Quaternion start_rot, Quaternion end_rot, float speed)
  {
    float t = 0.0f;
    end_pos.y += 0.08f;
    Vector3 middle_pos = Vector3.Lerp(start_pos, end_pos, 0.5f);
    middle_pos.y += 0.2f;

    while(true)
    {
      if ( t >= 1 )
      {
        if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CabinetEnterState)
        {
          _CabinetAnimator.CrossFade(CabinetCloseState, 0.1f, 0, 0);
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CabinetAttackState)
        {
          _Animator.CrossFade(IdleState, 0.3f, 0, 0);
          _CabinetAnimator.CrossFade(CabinetCloseState, 0.1f, 0, 0);
          _Ctrl.enabled = true;
        }
        else if(_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CabinetExitJumpState)
        {
          _Animator.CrossFade(LandingState, 0.1f, 0, 0);
          _CabinetAnimator.CrossFade(CabinetCloseState, 0.1f, 0, 0);
          _Ctrl.enabled = true;
        }
        yield break;
      }
      
      t += speed * Time.deltaTime;
      float c = -1 * t * (t - 2.0f);
      _Animator.SetFloat(JumpPoseParameter, t);
      Vector3 a = Vector3.Lerp(start_pos, middle_pos, t);
      Vector3 b = Vector3.Lerp(middle_pos, end_pos, t);
      this.transform.position = Vector3.Lerp(a, b, c);

      this.transform.rotation = Quaternion.Lerp(start_rot, end_rot, t);

      yield return null;
    }
  }
  //---------------------------------------------------------------------
  // Movement when exiting the cabinet
  //---------------------------------------------------------------------
  private void EXIT_CABINET ()
  {
    if (Input.GetKeyUp(KeyCode.V)
        && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash == CabinetIdleState
        && !_Animator.IsInTransition(0))
    {
      if(Input.GetKey(KeyCode.Z))
      {
        _Animator.Play(CabinetAttackState);
        _CabinetAnimator.CrossFade(CabinetAttackState, 0.1f, 0, 0);
        _CabinetAnimator.SetFloat(AnimationSpeedParameter, 1);
        Vector3 velocity = this.transform.rotation * new Vector3(0, 0, 1);
        Vector3 end_pos = this.transform.position + velocity;
        end_pos.y = 0;
        StartCoroutine(CharacterMove(this.transform.position, end_pos, this.transform.rotation, this.transform.rotation, 2));
      }
      else if(!Input.GetKey(KeyCode.Z))
      {
        if(!Input.GetKey(KeyCode.LeftShift))
        {
          _Animator.CrossFade(CabinetExit1State, 0.1f, 0, 0);
          _CabinetAnimator.CrossFade(CabinetExitState, 0.1f, 0, 0);
          _CabinetAnimator.SetFloat(AnimationSpeedParameter, 0.8f);
        }
        else if(Input.GetKey(KeyCode.LeftShift))
        {
          _Animator.CrossFade(CabinetExit2State, 0.1f, 0, 0);
          _CabinetAnimator.CrossFade(CabinetExitState, 0.1f, 0, 0);
          _CabinetAnimator.SetFloat(AnimationSpeedParameter, 0.5f);
        }
      }
    }
  }
  //---------------------------------------------------------------------
  // OnTrigger method
  //---------------------------------------------------------------------
  void OnTriggerEnter(Collider other)
  {
    // method so that the character hang to cliff
    if(other.gameObject.name == "HillEdge")
    {
      if(!CheckGrounded())
      {
        _Animator.CrossFade(HangState, 0.1f, 0, 0);
        Vector3 pos = new Vector3(_HillEdge.transform.position.x, _HillEdge.transform.position.y, this.transform.position.z);
        _Ctrl.enabled = false;
        this.transform.position = pos;
        _Ctrl.enabled = true;
        this.transform.rotation = Quaternion.Euler(0,270,0);
      }
    }
    // touched the sticky panel
    if(other.gameObject.name == "StickyPanel" && _Animator.GetFloat(DamageParameter) < 2 && _Animator.GetFloat(PoseParameter) == 0)
    {
      _Animator.CrossFade(DamageStickyState, 0.1f, 0, 0);
      _TextInfo.text = "Press R Key";
      _StatusIconImage.sprite = _StatusIconSticky;
      _StatusIconImage.color = new Color (0.9f, 0.9f, 0.1f);
    }
    // touched the trap panel
    if(other.gameObject.name == "Trap"  && _Animator.GetFloat(DamageParameter) < 2 && _Animator.GetFloat(PoseParameter) == 0)
    {
      _Animator.CrossFade(DamageTrappedState, 0.1f, 0, 0);
      _TrapAnimator.CrossFade(CloseState, 0.1f, 0, 0);
      _TextInfo.text = "Press R Key";
      _StatusIconImage.sprite = _StatusIconTrapped;
      _StatusIconImage.color = new Color (0.9f, 0.9f, 0.1f);
      _BloodIcon.gameObject.SetActive(true);
    }
    if(other.gameObject.name == "KickObj" && !_HaveItem)
    {
      _TextInfo.text = "Press K Key";
    }
    if(other.gameObject.name == "ObjHammer" && !_HaveItem)
    {
      _TextInfo.text = "Press W Key";
    }
    if(other.gameObject.name == "ObjSmokebomb" && !_HaveItem)
    {
      _TextInfo.text = "Press W Key";
    }
    if(other.gameObject.name == "ObjSlingshot" && !_HaveItem)
    {
      _TextInfo.text = "Press W Key";
    }
    if(other.gameObject.name == "ObjBook" && !_HaveItem)
    {
      _TextInfo.text = "Press W Key";
    }
    if(other.gameObject.name == "ObjBandage" && !_HaveItem)
    {
      _TextInfo.text = "Press W Key";
    }
    if(other.gameObject.name == "ObjFlashlight" && !_HaveItem)
    {
      _TextInfo.text = "Press W Key";
    }
    if(other.gameObject.name == "BoyNPC" && !_HaveItem)
    {
      _TextInfo.text = "Press G or H Key";
    }
    if(other.gameObject.name == "eventObj" && _HaveBook)
    {
      _TextInfo.text = "Press F Key";
    }
    if(other.gameObject.name == "Switch")
    {
      _TextInfo.text = "Press V Key";
    }
    if(other.gameObject.name == "Board")
    {
      _TextInfo.text = "Press V Key";
    }
    if(other.gameObject.name == "Cabinet")
    {
      _TextInfo.text = "Press V Key";
    }
    if(other.gameObject.name == "Cabinet2")
    {
      _TextInfo.text = "Press V Key";
    }
  }
  void OnTriggerExit(Collider other)
  {
    if(other.gameObject.name == "KickObj" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "ObjHammer" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "ObjSmokebomb" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "ObjSlingshot" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "ObjBook" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "ObjBandage" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "ObjFlashlight" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "BoyNPC" && !_HaveItem)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "eventObj" && _HaveBook)
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "Switch")
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "Board")
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "Cabinet")
    {
      _TextInfo.text = "";
    }
    if(other.gameObject.name == "Cabinet2")
    {
      _TextInfo.text = "";
    }
  }
  void OnTriggerStay(Collider other)
  {
    // Actions on objects
    if(CheckGrounded())
    {
      if(other.gameObject.name == "KickObj")
      {
        if (Input.GetKeyDown(KeyCode.K) && !_Animator.IsInTransition(0))
        {
          _Animator.CrossFade(KickState, 0.1f, 0, 0);
          // rotate Character
          Vector3 target = _KickObj.transform.position;
          target.y = this.transform.position.y;
          this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
        }
      }
      if(other.gameObject.name == "BoyNPC")
      {
        if(Input.GetKeyDown(KeyCode.G)
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != PsychicState)
        {
          _Animator.CrossFade(TreatState, 0.1f, 0, 0);
          _BoyNPCAnimator.CrossFade(TreatedState, 0.1f, 0, 0);
          _Slider.gameObject.SetActive(true);
          _SliderGauge = 0;
        }
        if(Input.GetKeyDown(KeyCode.H)
            && _Animator.GetCurrentAnimatorStateInfo(0).fullPathHash != TreatState)
        {
          _Animator.CrossFade(PsychicState, 0.1f, 0, 0);
          _BoyNPCAnimator.CrossFade(CaughtState, 0.1f, 0, 0);
          _BoyNPC.transform.position = _BoyNPC.transform.position + new Vector3(0,1,0);
        }
      }
      if(other.gameObject.name == "eventObj")
      {
        if(_HaveItem && _HaveBook)
        {
          if (Input.GetKeyDown(KeyCode.F) && !_Animator.IsInTransition(0))
          {
            _Animator.CrossFade(BookState, 0.1f, 0, 0);
            _Book.gameObject.SetActive(true);
            _SliderGauge = 0;
            Vector3 target = other.gameObject.transform.position;
            target.y = this.transform.position.y;
            this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
          }
          if (Input.GetKeyUp(KeyCode.F))
          {
            if(_SliderGauge >= 1.0f)
            {
              _Animator.CrossFade(BookReaction2State, 0.05f, 0, 0);
              _EventObjAnimator.CrossFade(KeyState, 0.1f, 0, 0);
              _EventObjCollider.enabled = false;
              GameObject effect = Instantiate<GameObject>(_EffectStoneBreak);
              effect.transform.position = _EventObjStone.transform.position;
              _EventObjStone.gameObject.SetActive(false);
            }
            else if(_SliderGauge < 1.0f)
            {
              _Animator.CrossFade(BookReaction1State, 0.1f, 0, 0);
            }
          }
        }
        if (Input.GetKey(KeyCode.F) && _HaveBook)
        {
          _SliderGauge += 0.5f * Time.deltaTime;
          float per = REMAP_VALUE(_SliderGauge, 0, 1, 0.07f, 0.56f);
          _EventObjMaterial.material.SetFloat("_Float", per);
          _EventObjMaterial.material.SetFloat("_GradientColor", _SliderGauge);
        }
      }
      if(other.gameObject.name == "Switch")
      {
        if (Input.GetKeyDown(KeyCode.V) && !_Animator.IsInTransition(0))
        {
          _Animator.CrossFade(SwitchState, 0.1f, 0, 0);
          _SwitchAnimator.SetTrigger(TriggerSwitchParameter);
          if(_SwitchAnimator.GetFloat(LRParameter) == 0)
          {
            _Animator.SetFloat(LRParameter, 1);
            _SwitchAnimator.SetFloat(LRParameter, 1);
            _Ctrl.enabled = false;
            this.transform.position = other.gameObject.transform.position + new Vector3(0.2f, 0, 0.3f);
            this.transform.rotation = Quaternion.Euler(0,180,0);
            _Ctrl.enabled = true;
          }
          else if(_SwitchAnimator.GetFloat(LRParameter) == 1)
          {
            _Animator.SetFloat(LRParameter, 0);
            _SwitchAnimator.SetFloat(LRParameter, 0);
            _Ctrl.enabled = false;
            this.transform.position = other.gameObject.transform.position + new Vector3(-0.2f, 0, 0.3f);
            this.transform.rotation = Quaternion.Euler(0,180,0);
            _Ctrl.enabled = true;
          }
        }
      }
      if(other.gameObject.name == "Board")
      {
        if (Input.GetKeyDown(KeyCode.V)
            && _BoardAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash != BoardAttackState
            && !_Animator.IsInTransition(0))
        {
          _Animator.CrossFade(BoardAttackState, 0.1f, 0, 0);
          _BoardAnimator.CrossFade(BoardAttackState, 0.1f, 0, 0);
          
          _Animator.SetFloat(LRParameter, 0);
          _Ctrl.enabled = false;
          this.transform.position = other.gameObject.transform.position + new Vector3(0, 0, 0.8f);
          this.transform.rotation = Quaternion.Euler(0,180,0);
          _Ctrl.enabled = true;
          _BoardCollider.center = new Vector3(0.5f,1,0);
          _BoardCollider.size = new Vector3(2,2,1.5f);
        }
        if (Input.GetKeyDown(KeyCode.V)
            && _BoardAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash == BoardAttackState
            && !_Animator.IsInTransition(0))
        {
          _Animator.CrossFade(KickState, 0.1f, 0, 0);
          _BoardAnimator.CrossFade(BoardBrokenState, 0.1f, 0, 0);
          
          _Animator.SetFloat(LRParameter, 0);
          _Ctrl.enabled = false;
          //this.transform.position = other.gameObject.transform.position + new Vector3(0, 0, 0.8f);
          Vector3 target = other.gameObject.transform.position;
          target.y = this.transform.position.y;
          this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
          _Ctrl.enabled = true;
          _BoardCollider.enabled = false;
        }
      }
      if(other.gameObject.name == "Cabinet")
      {
        if (Input.GetKeyDown(KeyCode.V)
            && !_Animator.IsInTransition(0))
        {
          _Animator.CrossFade(CabinetOpenState, 0.1f, 0, 0);
          _CabinetAnimator.CrossFade(CabinetOpenState, 0.1f, 0, 0);

          _Ctrl.enabled = false;
          this.transform.position = other.gameObject.transform.position + new Vector3(0, 0, 0.7f);
          Vector3 target = other.gameObject.transform.position;
          target.y = this.transform.position.y;
          this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
          StartPos = this.transform.position;
          EndPos = other.gameObject.transform.position;
          StartRot = this.transform.rotation;
          EndRot = other.gameObject.transform.rotation;
        }
      }
      if(other.gameObject.name == "Cabinet2")
      {
        if (Input.GetKeyDown(KeyCode.V)
            && !_Animator.IsInTransition(0))
        {
          _Animator.CrossFade(CabinetOpenMissState, 0.1f, 0, 0);
          _Cabinet2Animator.CrossFade(CabinetOpenMissState, 0.1f, 0, 0);
          _BoyNPC3Animator.CrossFade(CabinetSurprisedState, 0.1f, 0, 0);

          _Ctrl.enabled = false;
          this.transform.position = other.gameObject.transform.position + new Vector3(0, 0, 0.7f);
          Vector3 target = other.gameObject.transform.position;
          target.y = this.transform.position.y;
          this.transform.rotation = Quaternion.LookRotation(target - this.transform.position);
          _Ctrl.enabled = true;
        }
      }
    }
    // Actions on items
    if(CheckGrounded()
        && Input.GetKeyDown(KeyCode.W) && !_HaveItem
        && _Animator.GetCurrentAnimatorStateInfo(0).tagHash != ItemTag
        && !_Animator.IsInTransition(0))
    {
      // Substitute the distance between a character and items
      Vector3 pos1 = this.transform.position;
      Vector3[] pos2 = new Vector3[]{
        _ObjHammer.transform.position,
        _ObjSmokebomb.transform.position,
        _ObjSlingshot.transform.position,
        _ObjBook.transform.position,
        _ObjBandage.transform.position,
        _ObjFlashlight.transform.position,
      };
      float[] distance = new float[pos2.Length];
      for(int i = 0; i < pos2.Length; i++){
        distance[i] = (pos1.x - pos2[i].x)*(pos1.x - pos2[i].x)+(pos1.y - pos2[i].y)*(pos1.y - pos2[i].y)+(pos1.z - pos2[i].z)*(pos1.z - pos2[i].z);
      }
      // Search for minimum value
      float min_value = 10000;
      for (int i=0; i < distance.Length; i++) {
        if (min_value > distance[i]) {
          min_value = distance[i];
        }
      }
      // get items
      if(distance[0] <= min_value)
      {
        if(other.gameObject.name == "ObjHammer")
        {
          _Animator.CrossFade(ItemGetState, 0.1f, 0, 0);
          _ObjItem = _ObjHammer;
          _ObjHammer.gameObject.SetActive(false);
          _TextInfo.text = "";
          _HaveHammer = true;
          _Item1Icon.gameObject.SetActive(true);
          _Item1IconImage.sprite = _ItemIconHammer;
        }
      }
      else if(distance[1] <= min_value)
      {
        if(other.gameObject.name == "ObjSmokebomb")
        {
          _Animator.CrossFade(ItemGetState, 0.1f, 0, 0);
          _ObjItem = _ObjSmokebomb;
          _ObjSmokebomb.gameObject.SetActive(false);
          _TextInfo.text = "";
          _HaveSmokebomb = true;
          _Item1Icon.gameObject.SetActive(true);
          _Item1IconImage.sprite = _ItemIconSmokebomb;
        }
      }
      else if(distance[2] <= min_value)
      {
        if(other.gameObject.name == "ObjSlingshot")
        {
          _Animator.CrossFade(ItemGetState, 0.1f, 0, 0);
          _ObjItem = _ObjSlingshot;
          _ObjSlingshot.gameObject.SetActive(false);
          _TextInfo.text = "";
          _HaveSlingshot = true;
          _Item1Icon.gameObject.SetActive(true);
          _Item1IconImage.sprite = _ItemIconSlingshot;
        }
      }
      else if(distance[3] <= min_value)
      {
        if(other.gameObject.name == "ObjBook")
        {
          _Animator.CrossFade(ItemGetState, 0.1f, 0, 0);
          _ObjItem = _ObjBook;
          _ObjBook.gameObject.SetActive(false);
          _TextInfo.text = "";
          _HaveBook = true;
          _Item1Icon.gameObject.SetActive(true);
          _Item1IconImage.sprite = _ItemIconBook;
        }
      }
      else if(distance[4] <= min_value)
      {
        if(other.gameObject.name == "ObjBandage")
        {
          _Animator.CrossFade(ItemGetState, 0.1f, 0, 0);
          _ObjItem = _ObjBandage;
          _ObjBandage.gameObject.SetActive(false);
          _TextInfo.text = "";
          _HaveBandage = true;
          _Item1Icon.gameObject.SetActive(true);
          _Item1IconImage.sprite = _ItemIconBandage;
        }
      }
      else if(distance[5] <= min_value)
      {
        if(other.gameObject.name == "ObjFlashlight")
        {
          _Animator.CrossFade(ItemGetState, 0.1f, 0, 0);
          _ObjItem = _ObjFlashlight;
          _ObjFlashlight.gameObject.SetActive(false);
          _TextInfo.text = "";
          _HaveFlashlight = true;
          _Item1Icon.gameObject.SetActive(true);
          _Item1IconImage.sprite = _ItemIconFlashlight;
        }
      }
    }
  }
  //---------------------------------------------------------------------
  // Animation Event
  //---------------------------------------------------------------------
  // Function (move_run)
  // instantiate footstep effect
  private void FootstepEffect()
  {
    GameObject effect = Instantiate<GameObject>(_EffectFootstep);
    effect.transform.position = this.transform.position + this.transform.rotation * new Vector3(0, 0, -0.05f);
  }
  // Function (action_smokebomb)
  // instantiate smokebomb effect
  private void SmokeEffect ()
  {
    _Smokebomb.gameObject.SetActive(false);
    GameObject effect = Instantiate<GameObject>(_EffectSmoke);
    effect.transform.position = this.transform.position;
    _HaveItem = false;
    _HaveSmokebomb = false;
    _ObjItem.gameObject.SetActive(true);
    _ObjItem.transform.position = new Vector3(0, 0.25f, -1);
    _Item1Icon.gameObject.SetActive(false);
  }
  // Function (action_cabinet_open)
  // Move the character when the character enters the cabinet.
  private void EnterCabinet ()
  {
    _Animator.CrossFade(CabinetEnterState, 0.1f, 0, 0);
    StartCoroutine(CharacterMove(StartPos, EndPos, StartRot, EndRot, 1));
  }
  // Function (action_cabinet_exit1)(action_cabinet_exit2)
  // Movement when exiting the cabinet
  private void JumpingOutFromCabinet ()
  {
    _Animator.CrossFade(CabinetExitJumpState, 0.1f, 0, 0);
    Vector3 velocity = this.transform.rotation * new Vector3(0, 0, 1);
    Vector3 end_pos = this.transform.position + velocity;
    end_pos.y = 0;
    StartCoroutine(CharacterMove(this.transform.position, end_pos, this.transform.rotation, this.transform.rotation, 2));
  }
}
}
