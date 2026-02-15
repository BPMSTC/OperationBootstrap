<?php

use Illuminate\Support\Facades\Route;
use Inertia\Inertia;

Route::redirect('/', '/category-groups');

// Inertia/React pages (UI routes)
Route::get('/category-groups', fn () => Inertia::render('CategoryGroups/Index'))
    ->name('pages.category-groups.index');

Route::get('/categories', fn () => Inertia::render('Categories/Index'))
    ->name('pages.categories.index');
