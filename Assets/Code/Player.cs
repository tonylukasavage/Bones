using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	[Header("Components")]
	public Rigidbody2D rb;
	public LayerMask groundLayer;

	[Header("Horizontal Movement")]
	public Vector2 direction;
	public float moveSpeed = 1;
	public float maxSpeed = 15f;
	public float maxFallSpeed = -30f;
	public float normalDecay = 0.975f;
	public float idleDecay = 0.92f;
	public float stopThreshold = 2f;
	public bool facingRight = true;

	[Header("Vertical Movement")]
	public float jumpSpeed = 20f;
	public float jumpDelay = 0.25f;
	public float jumpGravity = 6f;
	public float fallGravity = 9f;
	private float jumpTimer;
	public float stopHorizontalTimer = 0.25f;
	public float stopRightTimer = 0;
	public float stopLeftTimer = 0;

	[Header("Collision")]
	public bool onGround = false;
	public bool onWall = false;
	public bool onWallRight = false;
	public bool onWallLeft = false;
	public bool isWallJumpingRight = false;
	public bool isWallJumpingLeft = false;
	public bool isWallSliding = false;
	public bool isFalling = false;
	public Vector3 baseOffset = new Vector3(0f, -1f, 0f);
	public Vector3 footOffset = new Vector3(0f, 0.13f, 0f);
	public Vector3 raycastDownOffset = new Vector3(0.5f, 0f, 0f);
	public Vector3 raycastWallOffset = new Vector3(0.89f, 0f, 0f);
	public float raycastDownLength = 0.2f;
	public float raycastWallLength = 0.3f;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		// set controller direction
		direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		// are we touching the ground
		Vector3 pos = transform.position + baseOffset;
		bool wasOnGround = onGround;
		onGround = Physics2D.Raycast(pos + raycastDownOffset, Vector2.down, raycastDownLength, groundLayer) ||
				Physics2D.Raycast(pos - raycastDownOffset, Vector2.down, raycastDownLength, groundLayer);

		// are we touching a wall
		pos = transform.position + baseOffset + footOffset;
		onWallRight = Physics2D.Raycast(pos + raycastWallOffset, Vector2.right, raycastWallLength, groundLayer);
		onWallLeft = Physics2D.Raycast(pos - raycastWallOffset, Vector2.left, raycastWallLength, groundLayer);
		onWall = onWallLeft || onWallRight;
		isWallSliding = !onGround && onWall && rb.velocity.y <= 0 &&
				((onWallLeft && direction.x < 0) || (onWallRight && direction.x > 0));

		// are we falling
		isFalling = rb.velocity.y < 0;

		if (onGround || onWall)
        {
			stopRightTimer = stopLeftTimer = 0f;
        } 
		
		// decrement horizontal stop timers
		if (stopRightTimer > 0)
        {
			stopRightTimer -= Time.deltaTime;
        }

		if (stopLeftTimer > 0)
		{
			stopLeftTimer -= Time.deltaTime;
		}

		// update jump timer
		if (Input.GetButtonDown("Jump"))
		{
			jumpTimer = Time.time + jumpDelay;
		}

	}

	void FixedUpdate()
	{
		modifyPhysics();
		moveCharacter(direction.x);
		if ((jumpTimer > Time.time) && (onGround || onWall))
		{
			Jump();
		}
	}

	void modifyPhysics()
    {
		// set gravity scale
		rb.gravityScale = onGround ? 1 : (isFalling ? fallGravity : jumpGravity);

		// wallslide
		if (isFalling && ((onWallLeft && direction.x < 0) ||(onWallRight && direction.x > 0)))
        {
			rb.gravityScale = 1.5f;
        } 

		// jump control
		if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
			rb.gravityScale = jumpGravity * 2.5f;
        }
    }

	void moveCharacter(float horizontal)
	{
		// handle wall jumping
		if (stopLeftTimer > 0)
        {
			horizontal = horizontal > 0 ? horizontal : 0;
        } else if (stopRightTimer > 0)
        {
			horizontal = horizontal < 0 ? horizontal : 0;
		}

		// determine movement decay
		bool changingDirection = (horizontal > 0 && rb.velocity.x < 0) || (horizontal < 0 && rb.velocity.x > 0);
		float decay = normalDecay;
		if (changingDirection)
		{
			decay = idleDecay;
		}
		else if (horizontal == 0)
		{
			decay = idleDecay;
			if (Mathf.Abs(rb.velocity.x) < stopThreshold)
			{
				rb.velocity = new Vector2(0, rb.velocity.y);
			}
		}

		// add horizontal force
		rb.velocity = new Vector2(rb.velocity.x * decay, rb.velocity.y);
		rb.AddForce(Vector2.right * horizontal * moveSpeed, ForceMode2D.Impulse);

		// flip sprite if needed
		if ((horizontal > 0 && !facingRight) || (horizontal < 0 && facingRight))
		{
			Flip();
		}

		// limit max speeds
		float xSpeed = Mathf.Abs(rb.velocity.x) > maxSpeed ? Mathf.Sign(rb.velocity.x) * maxSpeed : rb.velocity.x;
		float ySpeed = rb.velocity.y < maxFallSpeed ? maxFallSpeed : rb.velocity.y;
		rb.velocity = new Vector2(xSpeed, ySpeed);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;

		// ground check
		Vector3 pos = transform.position + baseOffset;
		Gizmos.DrawLine(pos + raycastDownOffset, pos + raycastDownOffset + Vector3.down * raycastDownLength);
		Gizmos.DrawLine(pos - raycastDownOffset, pos - raycastDownOffset + Vector3.down * raycastDownLength);

        // wall check
        pos = transform.position + baseOffset + footOffset;
        Gizmos.DrawLine(pos + raycastWallOffset, pos + raycastWallOffset + Vector3.right * raycastWallLength);
        Gizmos.DrawLine(pos - raycastWallOffset, pos - raycastWallOffset + Vector3.left * raycastWallLength);
    }

	void Flip()
	{
		facingRight = !facingRight;
		baseOffset = Vector3.Scale(baseOffset, new Vector3(-1, 1, 1));
		transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
	}

	void Jump()
    {
        if (onGround)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
        }
        else if (onWall)
        {
            rb.velocity = new Vector2(0, 0);
            Vector2 dir = onWallRight ? Vector2.left : Vector2.right;
            rb.AddForce(Vector2.up * jumpSpeed, ForceMode2D.Impulse);
            rb.AddForce(dir * moveSpeed * 20, ForceMode2D.Impulse);
			if (onWallLeft)
            {
				stopLeftTimer = stopHorizontalTimer;
				stopRightTimer = 0;
            } else
            {
				stopRightTimer = stopHorizontalTimer;
				stopLeftTimer = 0;
            }
        }
        jumpTimer = 0;
    }
}
