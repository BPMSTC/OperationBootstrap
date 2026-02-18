<?php

namespace App\Http\Requests\Api\CategoryGroup;

use Illuminate\Foundation\Http\FormRequest;
use Illuminate\Validation\Rule;

class UpdateCategoryGroupRequest extends FormRequest
{
    public function authorize(): bool
    {
        return true;
    }

    public function rules(): array
    {
        $groupId = $this->route('category_group')?->id ?? $this->route('category_group');

        return [
            'name' => [
                'sometimes',
                'required',
                'string',
                'max:100',
                Rule::unique('category_groups', 'name')
                    ->ignore($groupId)
                    ->whereNull('deleted_at'),
            ],
            'sort_order' => ['sometimes', 'nullable', 'integer'],
            'is_active' => ['sometimes', 'boolean'],

            'updated_by_user_id' => ['nullable', 'integer'],
        ];
    }
}
