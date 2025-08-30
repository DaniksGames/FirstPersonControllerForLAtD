using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]



public class PlayerController : MonoBehaviour
{
    private bool WalkFlag = false;
    private string GroundTag;
    public AudioClip Walk;
    public AudioClip Jump;
    public AudioClip Wall;
    public AudioSource audioSource;
    private float MaxY;
    public float Speed = 0.3f;
    private float SpeedBuf;
    public float JumpForce = 1f;

    private Vector3 previousPosition;
    private Vector3 currentVelocity;



    [SerializeField] private float sensitivity = 3; // ���������������� �����
    private float X, Y;

    //���� ����������� ������� ��� ����.
    //��� �� ��������� ��� ��� Player ��� �� ��������� � ������ ����. 

    //!!!!�������� �� ���� ������������� Layer, �������� Player!!!!
    public LayerMask GroundLayer; // 1 == "Default"
    public LayerMask WallLayer;

    private Rigidbody _rb;
    private Collider _collider;
    private Transform HeadTransform;

    private static GameObject blackScreen;

    public static void CreateBlackScreen(float power)
    {
        if (blackScreen != null) return;

        // ������� ������
        GameObject canvasObj = new GameObject("BlackScreenCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        // ������� Image
        GameObject imageObj = new GameObject("BlackImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;

        // ����������� �� ���� �����
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        blackScreen = canvasObj;
        image.color = new Color(0,0,0,power/4);
        DontDestroyOnLoad(blackScreen);
    }

    public static void RemoveBlackScreen()
    {
        if (blackScreen != null)
        {
            Destroy(blackScreen);
            blackScreen = null;
        }
    }
    private bool _isGrounded
    {
        get
        {
            var bottomCenterPoint = new Vector3(_collider.bounds.center.x, _collider.bounds.min.y, _collider.bounds.center.z);

            //������� ��������� ���������� ������� � ��������� �� ���������� �� ��� ������ ������� ��������� � ����

            //_collider.bounds.size.x / 2 * 0.9f -- ��� �������� ����������� ����� ������ �������.
            // ��� �� ����������� ������ -- ������ �� ������ ��������, � ��� ����� ��-�������������

            return Physics.CheckCapsule(_collider.bounds.center, bottomCenterPoint, _collider.bounds.size.x / 2 * 0.6f, GroundLayer);
            // ���� ����� ����� ������� � �������, �� ����� ����� �������� ���������� 0.9 �� �������.
        }
    }

    public Vector3 CalculateVelocity(Vector3 currentPosition)
    {
        float deltaTime = Time.deltaTime;

        // ������ �� ������� �� ����
        if (deltaTime <= 0f)
            return Vector3.zero;

        // �������� = ��������� ������� / �����
        return (currentPosition - previousPosition) / deltaTime;
    }

    private Vector3 _movementVector
    {
        get
        {
            var horizontal = Input.GetAxis("Horizontal")* (0.75f);
            var vertical = Input.GetAxis("Vertical");
            if (vertical < 0) vertical *= 0.5f;
            Vector3 direction = (transform.forward * vertical) + (horizontal * transform.right);

            return direction;
        }
    }

    void Start()
    {
        previousPosition = transform.position;
        SpeedBuf = Speed;
        //GroundLayer.value = 6;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        HeadTransform = transform.Find("head").GetComponent<Transform>();

        //�.�. ��� �� ����� ��� �� �������� ��� ������ ��� ��-���� ��� ������ �� �� ��������.
        //�� ����� ��������� ������� �� ���� X � Z
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;


        if (GroundLayer == gameObject.layer)
            Debug.LogError("Player SortingLayer must be different from Ground SourtingLayer!");
    }

    private IEnumerator WalkSteps(float strength)
    {
        WalkFlag = true;
        if (GroundTag == "Dirt")
        audioSource.PlayOneShot(Walk);
        //else if (GroundTag ==
        if (strength >= 1)
            yield return new WaitForSeconds(2 / strength);
        else
            yield return new WaitForSeconds(3);
        WalkFlag = false;
    }

    private void Update()
    {
        
        if (!_isGrounded)
        {
            if (transform.position.y> MaxY)
            {
                MaxY = transform.position.y;
            } 
        }
        if (currentVelocity.magnitude != 0 && _isGrounded)
        {
            if (!WalkFlag)
            StartCoroutine(WalkSteps(currentVelocity.magnitude));
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Speed = SpeedBuf * 2;
        }
        else Speed = SpeedBuf;
        X = Input.GetAxis("Mouse X") * sensitivity;
        Y += Input.GetAxis("Mouse Y") * sensitivity;
        Y = Mathf.Clamp(Y, -45f, 45f);
        transform.localEulerAngles += new Vector3(0, X, 0);
        HeadTransform.localEulerAngles = new Vector3(-Y, 0, 0);
    }
    void FixedUpdate()
    {
        // ��������� ��������
        currentVelocity = CalculateVelocity(transform.position);

        // ��������� ���������� ������� ��� ���������� �����
        previousPosition = transform.position;
        JumpLogic();
        MoveLogic();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (Mathf.Pow(2, collision.gameObject.layer) == GroundLayer.value)
        {
            GroundTag = collision.gameObject.tag;
            StartCoroutine(Slow(MaxY-transform.position.y));
            //print(MaxY - transform.position.y);
            MaxY = 0;
        }
        if (Mathf.Pow(2, collision.gameObject.layer) == WallLayer.value)
        {
            audioSource.PlayOneShot(Wall);
        }
    }
    
    private IEnumerator Slow(float strength)
    {
        CreateBlackScreen(strength);
        var strengthBuf = strength;
        SpeedBuf /= strengthBuf;
        yield return new WaitForSeconds(0.25f* strength);
        SpeedBuf *= strengthBuf;
        RemoveBlackScreen();
    }

    private void MoveLogic()
    {
        // �.�. �� ������ ������ ������������ ���������� �������� �����,
        // �� ������ � ��������� Time.fixedDeltaTime
        //_rb.AddForce((_movementVector * Speed)+BoatRb.velocity, ForceMode.Impulse);
        _rb.MovePosition(_rb.position + _movementVector * Speed * Time.fixedDeltaTime);
    }

    private void JumpLogic()
    {
        //print(_isGrounded);
        if (_isGrounded && (Input.GetAxis("Jump") > 0))
        {
            _rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            audioSource.Play();
        }
    }
}