<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Http\Requests\Api\CategoryGroup\StoreCategoryGroupRequest;
use App\Http\Requests\Api\CategoryGroup\UpdateCategoryGroupRequest;
use App\Http\Resources\CategoryGroupResource;
use App\Models\CategoryGroup;
use Illuminate\Http\Request;

class CategoryGroupController extends Controller
{
    public function index(Request $request)
    {
        $include = collect(explode(',', (string) $request->query('include')))
            ->filter()
            ->map(fn ($s) => trim($s))
            ->unique();

        $query = CategoryGroup::query();

        if ($request->filled('is_active')) {
            $isActive = filter_var($request->query('is_active'), FILTER_VALIDATE_BOOLEAN, FILTER_NULL_ON_FAILURE);
            if ($isActive !== null) {
                $query->where('is_active', $isActive);
            }
        }

        if ($include->contains('categories')) {
            $query->with(['categories' => function ($q) {
                $q->orderByRaw('COALESCE(sort_order, 2147483647) asc')
                  ->orderBy('name');
            }]);
        }

        $query->orderByRaw('COALESCE(sort_order, 2147483647) asc')
              ->orderBy('name');

        return CategoryGroupResource::collection(
            $query->paginate(25)->withQueryString()
        );
    }

    public function store(StoreCategoryGroupRequest $request)
    {
        $group = CategoryGroup::create($request->validated());

        return (new CategoryGroupResource($group))
            ->response()
            ->setStatusCode(201);
    }

    public function show(Request $request, CategoryGroup $category_group)
    {
        $include = collect(explode(',', (string) $request->query('include')))
            ->filter()
            ->map(fn ($s) => trim($s))
            ->unique();

        if ($include->contains('categories')) {
            $category_group->load(['categories' => function ($q) {
                $q->orderByRaw('COALESCE(sort_order, 2147483647) asc')
                  ->orderBy('name');
            }]);
        }

        return new CategoryGroupResource($category_group);
    }

    public function update(UpdateCategoryGroupRequest $request, CategoryGroup $category_group)
    {
        $category_group->update($request->validated());

        return new CategoryGroupResource($category_group->fresh());
    }

    public function destroy(CategoryGroup $category_group)
    {
        $category_group->delete();

        return response()->noContent();
    }
}
