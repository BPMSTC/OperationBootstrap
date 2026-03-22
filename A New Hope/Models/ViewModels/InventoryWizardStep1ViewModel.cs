using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace A_New_Hope.Models.ViewModels
{
    /// <summary>
    /// InventoryWizardStep1ViewModel
    /// -----------------------------
    /// Holds the Step 1 draft data for the Inventory Item Wizard.
    ///
    /// Wizard v1 scope:
    /// - Add a new InventoryItem
    /// - Update an existing InventoryItem
    ///
    /// Important behavior:
    /// - No database writes occur in Step 1.
    /// - Step 1 collects and validates the draft only.
    /// - The controller stores the validated draft in Session.
    /// - Step 2 displays the draft as a read-only confirmation page.
    /// </summary>
    public class InventoryWizardStep1ViewModel
    {
        /// <summary>
        /// Determines which path the user is taking in the wizard.
        /// Expected values:
        /// - "Create"
        /// - "Update"
        /// </summary>
        [Display(Name = "Wizard Action")]
        [Required(ErrorMessage = "Please choose whether you are adding a new item or updating an existing item.")]
        public string ActionType { get; set; } = "Create";

        /// <summary>
        /// Used when the user chooses the "Update Existing Item" path.
        /// Null when creating a new item.
        /// </summary>
        [Display(Name = "Existing Inventory Item")]
        public ulong? ExistingInventoryItemId { get; set; }

        /// <summary>
        /// Indicates whether an existing item has been intentionally loaded
        /// into the Step 1 draft for editing.
        /// </summary>
        public bool IsExistingItemLoaded { get; set; }

        /// <summary>
        /// Inventory item name to create or update.
        /// Final controller validation should still normalize and validate this
        /// using the same business rules as the standard InventoryItemsController.
        /// </summary>
        [Display(Name = "Item Name")]
        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(150, ErrorMessage = "Item name cannot exceed 150 characters.")]
        public string? Name { get; set; }

        /// <summary>
        /// Category selected for the item.
        /// This maps directly to InventoryItem.CategoryId.
        /// </summary>
        [Display(Name = "Category")]
        [Required(ErrorMessage = "Please select a category.")]
        public ulong? CategoryId { get; set; }

        /// <summary>
        /// Indicates whether the item is part of the baseline inventory set.
        /// </summary>
        [Display(Name = "Baseline Item")]
        public bool IsBaseline { get; set; }

        /// <summary>
        /// Indicates whether the item is currently available for use/selection.
        /// </summary>
        [Display(Name = "Available")]
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Indicates whether the item is active in the business sense.
        /// </summary>
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // ---------------------------------------------------------
        // Optional display/helper properties for Step 2 confirmation
        // ---------------------------------------------------------

        /// <summary>
        /// Friendly display text for the selected existing inventory item.
        /// Helpful for Step 2 summary output.
        /// </summary>
        public string? ExistingInventoryItemDisplayName { get; set; }

        /// <summary>
        /// Friendly display text for the selected category.
        /// Helpful for Step 2 summary output.
        /// Example: "Food - Refrigerated"
        /// </summary>
        public string? CategoryDisplayName { get; set; }

        // ---------------------------------------------------------
        // Dropdown data for Step 1
        // ---------------------------------------------------------

        /// <summary>
        /// Existing active inventory items available for selection
        /// in the update path.
        /// </summary>
        public List<SelectListItem> ExistingInventoryItems { get; set; } = new();

        /// <summary>
        /// Available active categories for selection.
        /// These will likely be displayed using grouped labels such as:
        /// "Food - Refrigerated"
        /// "Non-Food - Hygiene"
        /// </summary>
        public List<SelectListItem> Categories { get; set; } = new();
    }
}