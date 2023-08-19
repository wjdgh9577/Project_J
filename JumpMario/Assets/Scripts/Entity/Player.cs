using System;
using UnityEngine;
using Runningboy.Collection;
using Runningboy.Manager;
using Sirenix.OdinInspector;
using System.Collections;

namespace Runningboy.Entity
{
    public class Player : Entity, IPlayable
    {
        [Header("Components")]
        [SerializeField]
        LineRenderer _arrow;

        [Header("Control Values")]
        [SerializeField, ReadOnly]
        EntityStatus _status = EntityStatus.Idle;
        [SerializeField]
        float _minRange = 0.1f;
        [SerializeField]
        float _maxRange = 5f;
        [SerializeField]
        float _forceRatio = 100;
        [SerializeField]
        float _superJumpAllowable = 1f;

        protected override void Reset()
        {
            base.Reset();
            _arrow = GetComponent<LineRenderer>();

            _status = EntityStatus.Idle;
            _minRange = 0.1f;
            _maxRange = 5f;
            _forceRatio = 100;
            _superJumpAllowable = 1f;
        }

        private void OnEnable()
        {
            GameManager instance = GameManager.instance;
            instance.onBeginDrag += OnBeginDrag;
            instance.onDuringDrag += OnDuringDrag;
            instance.onEndDrag += OnEndDrag;

            tag = "Player";
        }

        private void OnDisable()
        {
            GameManager instance = GameManager.instance;
            instance.onBeginDrag -= OnBeginDrag;
            instance.onDuringDrag -= OnDuringDrag;
            instance.onEndDrag -= OnEndDrag;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            switch (collision.gameObject.tag)
            {
                case "Ground":
                    foreach (var contact in  collision.contacts)
                    {
                        if (contact.normal.y > 0)
                            goto _loop;
                    }
                    break;
                _loop:
                    SetStatus(EntityStatus.Idle);
                    break;
                default:
                    break;
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            switch (collision.gameObject.tag)
            {
                case "Ground":
                    if ((_status & CanJump) != 0)
                    {
                        SetStatus(EntityStatus.Jump);
                    }
                    break;
                default:
                    break;
            }
        }

        #region Drag Callbacks

        private void OnBeginDrag(object sender, EventArgs callback)
        {
            if (callback is DragCallbackArgs args)
            {
                if ((_status & EntityStatus.Idle) != 0)
                {
                    SetStatus(EntityStatus.Crouch);
                }
            }
        }

        private void OnDuringDrag(object sender, EventArgs callback)
        {
            if (callback is DragCallbackArgs args)
            {
                if ((_status & EntityStatus.Idle) != 0)
                {
                    SetStatus(EntityStatus.Crouch);
                }

                Vector3 start = GUIManager.instance.ScreenToWorldPoint(args.startScreenPosition);
                Vector3 currnet = GUIManager.instance.ScreenToWorldPoint(args.currentScreenPosition);

                RenderArrow(args.reverse ? currnet - start : start - currnet);
            }
        }

        private void OnEndDrag(object sender, EventArgs callback)
        {
            _arrow.enabled = false;

            if ((_status & CannotJump) != 0)
                return;

            if (callback is DragCallbackArgs args)
            {
                Vector3 start = GUIManager.instance.ScreenToWorldPoint(args.startScreenPosition);
                Vector3 currnet = GUIManager.instance.ScreenToWorldPoint(args.currentScreenPosition);

                Vector2 newVector = args.reverse ? currnet - start : start - currnet;
                Vector2 normalized = newVector.normalized;
                float range = MathF.Sqrt(newVector.sqrMagnitude);
                
                switch (_status)
                {
                    case EntityStatus.Idle:
                    case EntityStatus.Crouch:
                        if (range >= _minRange) // Jump or Slide
                        {
                            range = Mathf.Min(range, _maxRange);

                            AddForce(normalized, range * _forceRatio);
                            SetStatus(EntityStatus.Idle);
                        }
                        else // Cancel
                        {
                            SetStatus(EntityStatus.Idle);
                        }
                        break;
                    case EntityStatus.Jump:
                        if (range >= _minRange && Mathf.Abs(_rigidbody.velocity.y) <= _superJumpAllowable) // Super Jump
                        {
                            range = Mathf.Min(range, _maxRange);

                            AddForce(normalized, range * _forceRatio);
                            SetStatus(EntityStatus.SuperJump);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        private readonly EntityStatus CannotJump = EntityStatus.SuperJump | EntityStatus.Die;
        private readonly EntityStatus CanJump = EntityStatus.Idle | EntityStatus.Crouch;

        private void AddForce(Vector2 dir, float force)
        {
            _spriteRenderer.flipX = dir.x < 0;
            _rigidbody.velocity = dir * Mathf.Sqrt(force);
        }

        private void RenderArrow(Vector3 vector)
        {
            _arrow.enabled = true;

            float range = Mathf.Clamp(Mathf.Sqrt(vector.sqrMagnitude), 0, _maxRange);
            Vector3 normalized = vector.normalized;

            _arrow.endColor = range >= _minRange ? Color.green : Color.red;

            _arrow.SetPosition(1, normalized * range);
        }

        private void SetStatus(EntityStatus status)
        {
            _status = status;

            string trigger;
            switch (status)
            {
                case EntityStatus.Idle:
                    trigger = "Idle";
                    break;
                case EntityStatus.Crouch:
                    trigger = "Crouch";
                    break;
                case EntityStatus.Jump:
                    trigger = "Jump";
                    break;
                case EntityStatus.SuperJump:
                    trigger = "SuperJump";
                    break;
                case EntityStatus.Die:
                    trigger = "Die";
                    break;
                default:
                    trigger = "Default";
                    break;
            }

            SetTrigger(trigger);
        }
    }
}