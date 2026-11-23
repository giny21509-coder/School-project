using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Serialization;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// Requires a CharacterMovement ability. Makes the character move up to the specified MinimumDistance in the direction of the target. 
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Move Towards Target 2D")]
	//[RequireComponent(typeof(CharacterMovement))]
	public class AIActionMoveTowardsTarget2D : AIAction
	{
		/// if this is true, movement will be constrained to not overstep a certain distance to the target on the x axis
		[Tooltip("if this is true, movement will be constrained to not overstep a certain distance to the target on the x axis")]
		public bool UseMinimumXDistance = true;
		/// the minimum distance from the target this Character can reach on the x axis.
		[FormerlySerializedAs("MinimumDistance")] [Tooltip("the minimum distance from the target this Character can reach on the x axis.")]
		public float MinimumXDistance = 1f;

		[Header("Inner World / Visual Spacing")]
		[Tooltip("Stop using the visible body bounds of both characters, with braking room. Disabled keeps the original AI movement.")]
		public bool UseMinimumVisualGap = false;
		[Min(0f), Tooltip("Minimum XY gap between visible body rectangles. MinimumXDistance remains the minimum root-to-root distance.")]
		public float MinimumVisualGap = 0.5f;

		protected Transform _spacingOwner;
		protected Transform _spacingTarget;
		protected Transform _spacingTargetSource;
		protected SpriteRenderer _spacingOwnerRenderer;
		protected SpriteRenderer _spacingTargetRenderer;
		
		protected Vector2 _direction;
		protected CharacterMovement _characterMovement;
		protected int _numberOfJumps = 0;

		/// <summary>
		/// On init we grab our CharacterMovement ability
		/// </summary>
		public override void Initialization()
		{
			if(!ShouldInitialize) return;
			base.Initialization();
			_characterMovement = this.gameObject.GetComponentInParent<Character>()?.FindAbility<CharacterMovement>();
			_spacingOwner = this.gameObject.GetComponentInParent<Character>()?.transform;
			if (_spacingOwner == null) _spacingOwner = transform;
			_spacingOwnerRenderer = FindBodyRenderer(_spacingOwner);
		}

		/// <summary>
		/// On PerformAction we move
		/// </summary>
		public override void PerformAction()
		{
			Move();
		}

		/// <summary>
		/// Moves the character towards the target if needed
		/// </summary>
		protected virtual void Move()
		{
			if (_brain.Target == null)
			{
				if (UseMinimumVisualGap) _characterMovement?.SetMovement(Vector2.zero);
				return;
			}

			if (UseMinimumVisualGap && UseMinimumXDistance)
			{
				MoveWithVisualGap();
				return;
			}

			if (UseMinimumXDistance)
			{
				if (this.transform.position.x < _brain.Target.position.x)
				{
					_characterMovement.SetHorizontalMovement(1f);
				}
				else
				{
					_characterMovement.SetHorizontalMovement(-1f);
				}

				if (this.transform.position.y < _brain.Target.position.y)
				{
					_characterMovement.SetVerticalMovement(1f);
				}
				else
				{
					_characterMovement.SetVerticalMovement(-1f);
				}
            
				if (Mathf.Abs(this.transform.position.x - _brain.Target.position.x) < MinimumXDistance)
				{
					_characterMovement.SetHorizontalMovement(0f);
				}

				if (Mathf.Abs(this.transform.position.y - _brain.Target.position.y) < MinimumXDistance)
				{
					_characterMovement.SetVerticalMovement(0f);
				}
			}
			else
			{
				_direction = (_brain.Target.position - this.transform.position).normalized;
				_characterMovement.SetMovement(_direction);
			}
			
		}

		protected virtual void MoveWithVisualGap()
		{
			if (_characterMovement == null) return;
			if (_spacingOwner == null)
				_spacingOwner = gameObject.GetComponentInParent<Character>()?.transform ?? transform;
			if (_spacingOwnerRenderer == null) _spacingOwnerRenderer = FindBodyRenderer(_spacingOwner);
			if (_spacingTargetSource != _brain.Target)
			{
				_spacingTargetSource = _brain.Target;
				_spacingTarget = _brain.Target.GetComponentInParent<Character>()?.transform ?? _brain.Target;
				_spacingTargetRenderer = FindBodyRenderer(_spacingTarget);
			}
			Vector2 offset = _spacingTarget.position - _spacingOwner.position;
			float distance = offset.magnitude;
			Bounds ownBounds = BodyBounds(_spacingOwner, _spacingOwnerRenderer);
			Bounds targetBounds = BodyBounds(_spacingTarget, _spacingTargetRenderer);
			float gapX = Mathf.Max(0f, Mathf.Max(ownBounds.min.x - targetBounds.max.x, targetBounds.min.x - ownBounds.max.x));
			float gapY = Mathf.Max(0f, Mathf.Max(ownBounds.min.y - targetBounds.max.y, targetBounds.min.y - ownBounds.max.y));
			// CharacterMovement decelerates after receiving zero input. Start braking before the final gap.
			float brakeRoom = _characterMovement.Acceleration > 0f && _characterMovement.Deceleration > 0f
				? 2f * Mathf.Max(0f, _characterMovement.MovementSpeed) / _characterMovement.Deceleration : 0f;
			float stopGap = Mathf.Max(0f, MinimumVisualGap) + brakeRoom;
			bool closeEnough = distance <= Mathf.Max(0f, MinimumXDistance) + brakeRoom
				|| gapX * gapX + gapY * gapY <= stopGap * stopGap;
			// Use character roots instead of the offset AI child; sprite Z never changes the XY gap.
			_characterMovement.SetMovement(closeEnough ? Vector2.zero : offset.normalized);
		}

		protected static SpriteRenderer FindBodyRenderer(Transform actor)
		{
			Character character = actor.GetComponent<Character>();
			Transform model = character != null && character.CharacterModel != null
				? character.CharacterModel.transform : actor;
			SpriteRenderer selected = null;
			float largestArea = -1f;
			foreach (SpriteRenderer candidate in model.GetComponentsInChildren<SpriteRenderer>())
			{
				if (!candidate.enabled || candidate.sprite == null) continue;
				float area = candidate.bounds.size.x * candidate.bounds.size.y;
				if (area > largestArea) { largestArea = area; selected = candidate; }
			}
			return selected;
		}

		protected static Bounds BodyBounds(Transform actor, SpriteRenderer body)
		{
			if (body != null) return body.bounds;
			Bounds bounds = new Bounds(actor.position, Vector3.zero);
			bool found = false;
			foreach (Collider2D collider in actor.GetComponentsInChildren<Collider2D>())
			{
				if (!collider.enabled || collider.isTrigger) continue;
				if (!found) { bounds = collider.bounds; found = true; }
				else bounds.Encapsulate(collider.bounds);
			}
			return bounds;
		}

		/// <summary>
		/// On exit state we stop our movement
		/// </summary>
		public override void OnExitState()
		{
			base.OnExitState();

			_characterMovement?.SetHorizontalMovement(0f);
			_characterMovement?.SetVerticalMovement(0f);
		}
	}
}
