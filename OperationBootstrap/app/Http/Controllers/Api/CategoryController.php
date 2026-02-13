<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Http\Requests\Api\Category\StoreCategoryRequest;
use App\Http\Requests\Api\Category\UpdateCategoryRequest;
use App\Http\Resources\CategoryResource;
use App\Models\Category;
use Illuminate\Http\Request;

class CategoryController extends Controller
{
    public function index(Request $request)
    {
        $include = collect(explode(',', (string) $request->query('include')))
            ->filter()
            ->map(fn ($s) => trim($s))
            ->unique();

        $query = Category::query();

        if ($request->filled('category_group_id')) {
            $query->where('category_group_id', (int) $request->query('category_group_id'));
        }

        if ($request->has('parent_id')) {
            $parentId = $request->query('parent_id');
            if ($parentId === null || $parentId === 'null' || $parentId === '') {
                $query->whereNull('parent_id');
            } else {
                $query->where('parent_id', (int) $parentId);
            }
        }

        if ($request->filled('is_active')) {
            $isActive = filter_var($request->query('is_active'), FILTER_VALIDATE_BOOLEAN, FILTER_NULL_ON_FAILURE);
            if ($isActive !== null) {
                $query->where('is_active', $isActive);
            }
        }

        $with = [];
        foreach (['group', 'parent', 'children'] as $rel) {
            if ($include->contains($rel)) {
                $with[] = $rel;
            }
        }
        if (!empty($with)) {
            $query->with($with);
        }

        $query->orderByRaw('COALESCE(sort_order, 2147483647) asc')
              ->orderBy('name');

        return CategoryResource::collection(
            $query->paginate(25)->withQueryString()
        );
    }

    public function store(StoreCategoryRequest $request)
    {
        $category = Category::create($request->validated());

        return (new CategoryResource($category))
            ->response()
            ->setStatusCode(201);
    }

    public function show(Request $request, Category $category)
    {
        $include = collect(explode(',', (string) $request->query('include')))
            ->filter()
            ->map(fn ($s) => trim($s))
            ->unique();

        $with = [];
        foreach (['group', 'parent', 'children'] as $rel) {
            if ($include->contains($rel)) {
                $with[] = $rel;
            }
        }
        if (!empty($with)) {
            $category->load($with);
        }

        return new CategoryResource($category);
    }

    public function update(UpdateCategoryRequest $request, Category $category)
    {
        $category->update($request->validated());

        return new CategoryResource($category->fresh());
    }

    public function destroy(Category $category)
    {
        $category->delete();

        return response()->noContent();
    }
}
