using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Rigidbody2D))]
public class UnitSystem : MonoBehaviour
{
    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    [SerializeField] private UnitData data;

    public bool isFired { private set; get; } = false;
    public bool isMerging { private set; get; } = false;
    public bool inHole { private set; get; } = false;

    [SerializeField] private GameObject xMark;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        xMark = transform.GetChild(0).gameObject;
        xMark.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D _collision)
    {
        Merge(_collision);
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (!isFired) return;
        if (_collision.CompareTag("Hole")) inHole = true;
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (!isFired || isMerging || !inHole) return;
        if (_collision.CompareTag("Hole"))
        {
            sr.color = new Color32(100, 100, 100, 255);
            xMark.transform.rotation = Quaternion.identity;
            xMark.SetActive(true);
            GameManager.Instance?.GameOver();
        }
    }

    public void Shoot(Vector2 _impulse, bool _isShot = true)
    {
        rb.AddForce(_impulse * rb.mass, ForceMode2D.Impulse);

        isFired = true;
        col.isTrigger = false;

        if (_isShot) SoundManager.Instance?.PlaySFX("Shoot");
        EntityManager.Instance?.CountUp(data);
    }

    private void Merge(Collision2D _collision)
    {
        var other = _collision.gameObject.GetComponent<UnitSystem>();

        if (other == null) return;
        if (!other.CompareTag(tag)) return;
        if (other.GetID() != GetID()) return;
        if (other.isMerging || isMerging) return;
        if (other.GetInstanceID() < GetInstanceID()) return;

        isMerging = true;
        other.isMerging = true;

        var otherRb = _collision.rigidbody;

        Vector2 pA = rb.position;
        Vector2 pB = otherRb.position;
        Vector2 pM = (pA + pB) / 2f;

        Vector2 vA = GetVelocity();
        Vector2 vB = other.GetVelocity();
        Vector2 vM = (vA + vB) / 2f;

        EntityManager.Instance?.Despawn(other);
        EntityManager.Instance?.Despawn(this);

        int id = GetID();
        int score = GetScore();

        if (id != EntityManager.Instance?.GetFinal())
        {
            UnitSystem us = EntityManager.Instance?.Spawn(id + 1, pM);
            us.Shoot(vM, false);
        }

        GameManager.Instance?.ScoreUp(score);
        SoundManager.Instance?.PlaySFX(id + 1 != EntityManager.Instance?.GetFinal() ? "Merge" : "Flame");
    }

    #region SET
    public void SetData(UnitData _data)
    {
        data = _data;
        gameObject.name = data.Name;
        transform.localScale = Vector3.one * data.Scale;
        rb.mass = data.Mass;
        sr.sprite = data.Image;
    }

    public void SetOut() => inHole = false;
    #endregion

    #region GET
    public SpriteRenderer GetSR() => sr;
    public Rigidbody2D GetRB() => rb;
    public Vector2 GetVelocity() => rb.linearVelocity;
    public int GetID() => data.ID;
    public int GetScore() => data.Score;
    #endregion
}
