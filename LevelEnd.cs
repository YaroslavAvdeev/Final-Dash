using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;
    public Rigidbody2D rb;

    private float moveInput;
    private bool isGrounded;

    void Update()
    {
        // Рух
        moveInput = 0;
        if (Keyboard.current.aKey.isPressed) moveInput = -1;
        if (Keyboard.current.dKey.isPressed) moveInput = 1;

        // Чи на землі?
        isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        // Анімація бігу/стоячки
        animator.SetFloat("Speed", Mathf.Abs(moveInput));

        // Анімація стрибка
        animator.SetBool("IsJumping", !isGrounded);
    }
}
