<?php

namespace App\Http\Requests\Api\Category;

use App\Models\Category;
use Illuminate\Foundation\Http\FormRequest;
use Illuminate\Validation\Rule;

class StoreCategoryRequest extends FormRequest
{
    public function authorize(): bool
    {
        return true;
    }

    public function rules(): array
    {
        return [
            'category_group_id' => ['required', 'integer', 'exists:category_groups,id'],
            'name' => [
                'required',
                'string',
                'max:150',
                // matches UNIQUE(category_group_id, name) while ignoring soft-deleted rows
                Rule::unique('categories', 'name')
                    ->where(fn ($q) => $q
                        ->where('category_group_id', $this->input('category_group_id'))
                        ->whereNull('deleted_at')
                    ),
            ],
            'parent_id' => ['nullable', 'integer', 'exists:categories,id'],
            'sort_order' => ['nullable', 'integer'],
            'is_active' => ['sometimes', 'boolean'],

            'created_by_user_id' => ['nullable', 'integer'],
            'updated_by_user_id' => ['nullable', 'integer'],
        ];
    }

    public function withValidator($validator): void
    {
        $validator->after(function ($validator) {
            $parentId = $this->input('parent_id');
            $groupId = $this->input('category_group_id');

            if (!$parentId || !$groupId) {
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
