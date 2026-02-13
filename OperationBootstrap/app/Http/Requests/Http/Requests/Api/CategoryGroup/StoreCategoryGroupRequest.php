<?php

namespace App\Http\Requests\Api\CategoryGroup;

use Illuminate\Foundation\Http\FormRequest;
use Illuminate\Validation\Rule;

class StoreCategoryGroupRequest extends FormRequest
{
    public function authorize(): bool
    {
        return true; // add auth/policy later
    }

    public function rules(): array
    {
        return [
            'name' => [
                'required',
                'string',
                'max:100',
                Rule::unique('category_groups', 'name')->whereNull('deleted_at'),
            ],
            'sort_order' => ['nullable', 'integer'],
            'is_active' => ['sometimes', 'boolean'],

            // Optional auditing columns if you ever set them from API
            'created_by_user_id' => ['nullable', 'integer'],
            'updated_by_user_id' => ['nullable', 'integer'],
        ];
    }
}
