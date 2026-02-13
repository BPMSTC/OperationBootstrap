<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
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

        // Optional filter: is_active
        if ($request->filled('is_active')) {
            $isActive = filter_var($request->query('is_active'), FILTER_VALIDATE_BOOLEAN, FILTER_NULL_ON_FAILURE);
            if ($isActive !== null) {
                $query->where('is_active', $isActive);
            }
        }

        // Optional include: categories
        if ($include->contains('categories')) {
            $query->with(['categories' => function ($q) {
                $q->orderByRaw('COALESCE(sort_order, 2147483647) asc')
                  ->orderBy('name');
            }]);
        }

        $query->orderByRaw('COALESCE(sort_order, 2147483647) asc')
              ->orderBy('name');

        return response()->json(
            $query->paginate(25)->withQueryString()
        );
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

        return response()->json($category_group);
    }
}
