<?php

namespace App\Http\Requests\Api\Category;

use App\Models\Category;
use Illuminate\Foundation\Http\FormRequest;
use Illuminate\Validation\Rule;

class UpdateCategoryRequest extends FormRequest
{
    public function authorize(): bool
    {
        return true;
    }

    public function rules(): array
    {
        /** @var \App\Models\Category|null $category */
        $category = $this->route('category');
        $categoryId = $category?->id ?? $this->route('category');

        // When updating, category_group_id might not be sent.
        // Use input if present; otherwise use the current model value.
        $groupId = $this->filled('category_group_id')
            ? (int) $this->input('category_group_id')
            : (int) ($category?->category_group_id);

        return [
            'category_group_id' => ['sometimes', 'required', 'integer', 'exists:category_groups,id'],
            'name' => [
                'sometimes',
                'required',
                'string',
                'max:150',
                Rule::unique('categories', 'name')
                    ->ignore($categoryId)
                    ->where(fn ($q) => $q
                        ->where('category_group_id', $groupId)
                        ->whereNull('deleted_at')
                    ),
            ],
            'parent_id' => ['sometimes', 'nullable', 'integer', 'exists:categories,id'],
            'sort_order' => ['sometimes', 'nullable', 'integer'],
            'is_active' => ['sometimes', 'boolean'],

            'updated_by_user_id' => ['nullable', 'integer'],
        ];
    }

    public function withValidator($validator): void
    {
        $validator->after(function ($validator) {
            /** @var \App\Models\Category|null $category */
            $category = $this->route('category');

            $parentId = $this->input('parent_id', '__missing__');

            // If parent_id wasn't part of the request, don't validate it.
            if ($parentId === '__missing__') {
                return;
            }

            // parent_id can be null (clearing parent) — that is allowed.
            if ($parentId === null || $parentId === '' || $parentId === 'null') {
                return;
            }

            $parentId = (int) $parentId;

            // Determine the groupId (incoming or existing)
            $groupId = $this->filled('category_group_id')
                ? (int) $this->input('category_group_id')
                : (int) ($category?->category_group_id);

            if (!$groupId) {
                return;
            }

            // Prevent self-parenting
            if ($category && (int) $category->id === $parentId) {
                $validator->errors()->add('parent_id', 'A category cannot be its own parent.');
                return;
            }

            $parent = Category::query()
                ->where('id', $parentId)
                ->whereNull('deleted_at')
                ->first();

            if (!$parent) {
                return; // exists rule will handle missing
            }

            if ((int) $parent->category_group_id !== (int) $groupId) {
                $validator->errors()->add('parent_id', 'Parent category must belong to the same category group.');
            }
        });
    }
}
