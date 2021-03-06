using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace LD48
{
	/// <summary>
	/// 
	/// </summary>
	public class Inventory : MonoBehaviour
	{
		/// <summary>
		/// 
		/// </summary>
		public static Inventory Instance
		{
			get;
			private set;
		} = null;

		public const string ItemHoveredBoolField = "Item Hovered";
		public const string InteractableHoveredBoolField = "Interactable Hovered";
		public const string ItemCarriedBoolField = "Carrying Item";
		public const string HoveredItemChangedTrigger = "Item Changed";
		public const string IsInteractableBoolField = "Is Interactable";

		[SerializeField]
		OmiyaGames.Audio.SoundEffect pickUpSound;

		[Header("Raycast")]
		[SerializeField]
		float scanItemDistance = 5f;
		[SerializeField]
		LayerMask itemMasks = int.MaxValue;
		[SerializeField]
		string itemTag = "Item";
		[SerializeField]
		string interactiveTag = "Interactive";
		[SerializeField]
		float gapBetweenActionSeconds = 0.5f;

		[Header("HUD")]
		[SerializeField]
		Animator interactiveHud;
		[SerializeField]
		TextMeshProUGUI hoverLabel;
		[SerializeField]
		TextMeshProUGUI carryingItemLabel;
		[SerializeField]
		TextMeshProUGUI interactableLabel;

		[Header("Debug")]
		[SerializeField]
		[OmiyaGames.ReadOnly]
		Item carrying = null;
		[SerializeField]
		[OmiyaGames.ReadOnly]
		Item hoveredItem = null;
		[SerializeField]
		[OmiyaGames.ReadOnly]
		IInteractable hoveredInteraction = null;

		Ray rayCache = new Ray();
		RaycastHit hitCache;
		float lastSetAction = 0;

		#region Properties
		/// <summary>
		/// Indicates the item being carried
		/// </summary>
		public Item Carrying
		{
			get => carrying;
			private set
			{
				// Check if anything changed
				if(carrying != value)
				{
					carrying = value;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Item HoveredItem
		{
			get => hoveredItem;
			private set
			{
				// Check if anything changed
				if(hoveredItem != value)
				{
					UpdateHud(hoveredItem, value);
					hoveredItem = value;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public IInteractable HoveredInteraction
		{
			get => hoveredInteraction;
			private set
			{
				if(hoveredInteraction != value)
				{
					UpdateHud(hoveredInteraction, value);
					hoveredInteraction = value;
				}
			}
		}
		#endregion

		/// <summary>
		/// 
		/// </summary>
		private void Awake()
		{
			Instance = this;
		}

		/// <summary>
		/// Update is called once per frame.
		/// </summary>
		void Update()
		{
			bool hoveredItem = false, hoveredInteractable = false;
			// Check if we're NOT in the gap period
			if((Time.time - lastSetAction) > gapBetweenActionSeconds)
			{
				// Update ray
				rayCache.origin = transform.position;
				rayCache.direction = transform.forward;

				// Raycast for an item
				if(Physics.Raycast(rayCache, out hitCache, scanItemDistance, itemMasks, QueryTriggerInteraction.Collide) == true)
				{
					// Check if we're carrying an item
					if(Carrying == null)
					{
						// If not, check if the detected object is really an item
						if(hitCache.collider.CompareTag(itemTag) == true)
						{
							HoveredItem = hitCache.collider.GetComponent<Item>();
							hoveredItem = true;
						}
						else if((hitCache.rigidbody != null) && (hitCache.rigidbody.CompareTag(itemTag) == true))
						{
							HoveredItem = hitCache.rigidbody.GetComponent<Item>();
							hoveredItem = true;
						}
					}

					// Check if we've hovered on an item
					if(hoveredItem == false)
					{
						// Check if we're hovering over an interactive item
						if(hitCache.collider.CompareTag(interactiveTag) == true)
						{
							HoveredInteraction = hitCache.collider.GetComponent<IInteractable>();
							hoveredInteractable = true;
						}
						else if((hitCache.rigidbody != null) && (hitCache.rigidbody.CompareTag(itemTag) == true))
						{
							HoveredInteraction = hitCache.rigidbody.GetComponent<IInteractable>();
							hoveredInteractable = true;
						}
					}
				}
			}

			// Otherwise, indicate we're not hovering over anything
			if(hoveredItem == false)
			{
				HoveredItem = null;
			}
			if(hoveredInteractable == false)
			{
				HoveredInteraction = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnDestroy()
		{
			Instance = null;
		}

		/// <summary>
		/// 
		/// </summary>
		public void OnAction(InputAction.CallbackContext context)
		{
			// Listen for mouse up
			if(context.phase == InputActionPhase.Canceled)
			{
				// Check what the intended action is
				bool dropItem = true;
				if((Carrying == null) && (HoveredItem != null))
				{
					PickUpItem();
					dropItem = false;
				}
				else if(HoveredInteraction?.OnClick(this) == true)
				{
					// Temporarily disable any actions
					dropItem = false;
					lastSetAction = Time.time;
				}

				if((dropItem == true) && (Carrying != null))
				{
					DropItem();
				}
			}
			else if(Carrying != null)
			{
				// Check if we want to show the sillohette
				bool showSillohette = false;
				if(context.phase == InputActionPhase.Performed)
				{
					showSillohette = true;

					// First, check if we're hovering something we can interact with
					IInteractable.HoverInfo info = null;
					if((HoveredInteraction?.OnHover(this, out info) == true) && (info.displayIcon == IInteractable.HoverIcon.Interact))
					{
						showSillohette = false;
					}
				}

				// Otherwise, show the drop sillouhette
				Carrying.IsDropLocationVisible = showSillohette;
			}
		}

		/// <summary>
		/// Destroy the item the inventory is carrying
		/// </summary>
		public void DestroyCarryingItem()
		{
			if(Carrying)
			{
				Destroy(Carrying.gameObject);
				Carrying = null;
			}
		}

		#region Helpers
		/// <summary>
		/// 
		/// </summary>
		private void DropItem()
		{
			// Drop the current item we're Carrying
			Carrying.CurrentState = Item.State.Idle;
			Carrying.IsDropLocationVisible = false;
			Carrying.transform.position = Carrying.DropLocation;
			Carrying.transform.SetParent(Carrying.OriginalParent, true);

			// Set the property to null
			Carrying = null;

			// Play dropping item HUD animation
			interactiveHud.SetBool(ItemCarriedBoolField, false);

			// Temporarily disable any actions
			lastSetAction = Time.time;
		}

		/// <summary>
		/// 
		/// </summary>
		private void PickUpItem()
		{
			// Transfer the variable over
			Carrying = HoveredItem;
			HoveredItem = null;
			pickUpSound.Play();

			// Setup the item as in-inventory
			Carrying.CurrentState = Item.State.InInventory;
			Carrying.transform.SetParent(transform, true);
			Carrying.transform.localPosition = Vector3.zero;
			Carrying.transform.localScale = Vector3.one;
			Carrying.transform.localRotation = Quaternion.identity;

			// Update label, play picking up item HUD animation
			carryingItemLabel.text = Carrying.DisplayName;
			interactiveHud.SetBool(ItemCarriedBoolField, true);

			// Temporarily disable any actions
			lastSetAction = Time.time;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="oldItem"></param>
		/// <param name="newItem"></param>
		private void UpdateHud(Item oldItem, Item newItem)
		{
			// Set whether to show the hover information
			interactiveHud.SetBool(ItemHoveredBoolField, (newItem != null));

			// Check if there's an item
			if(newItem != null)
			{
				// Update text
				hoverLabel.text = newItem.DisplayName;

				// Check if hovered item simply changed
				if(oldItem != null)
				{
					// Indicate change
					interactiveHud.ResetTrigger(HoveredItemChangedTrigger);
					interactiveHud.SetTrigger(HoveredItemChangedTrigger);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="oldInteraction"></param>
		/// <param name="newInteraction"></param>
		private void UpdateHud(IInteractable oldInteraction, IInteractable newInteraction)
		{
			// Execute the hover action
			bool showInteraction = false;
			if(newInteraction && newInteraction.OnHover(this, out IInteractable.HoverInfo info))
			{
				// Set whether to show the hover information
				interactiveHud.SetBool(InteractableHoveredBoolField, true);
				interactiveHud.SetBool(IsInteractableBoolField, (info.displayIcon == IInteractable.HoverIcon.Interact));
				interactableLabel.text = info.displayInstructions;

				// FIXME: actually update the HUD
				showInteraction = (info.displayIcon == IInteractable.HoverIcon.Interact);
			}
			else
			{
				// Set whether to show the hover information
				interactiveHud.SetBool(InteractableHoveredBoolField, false);
			}

			// Check if we're carrying an item
			if(Carrying)
			{
				// Hide the drop item prompt if we could interact with something.
				// The interaction takes priority.
				interactiveHud.SetBool(ItemCarriedBoolField, !showInteraction);
			}
		}
		#endregion
	}
}
