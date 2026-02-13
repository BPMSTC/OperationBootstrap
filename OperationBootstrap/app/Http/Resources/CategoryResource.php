<?php

namespace App\Http\Resources;

use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;

class CategoryResource extends JsonResource
{
    public function toArray(Request $request): array
    {
        return [
            'id' => $this->id,
            'category_group_id' => $this->category_group_id,
            'name' => $this->name,
            'parent_id' => $this->parent_id,
            'sort_order' => $this->sort_order,
            'is_active' => (bool) $this->is_active,
            'created_at' => $this->created_at,
            'updated_at' => $this->updated_at,

            'group' => $this->whenLoaded('group', function () {
                return new CategoryGroupResource($this->group);
            }),

            'parent' => $this->whenLoaded('parent', function () {
                return new CategoryResource($this->parent);
            }),

            'children' => $this->whenLoaded('children', function () {
                return CategoryResource::collection($this->children);
            }),
        ];
    }
}
