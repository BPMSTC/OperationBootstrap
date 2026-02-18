<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\Api\CategoryGroupController;
use App\Http\Controllers\Api\CategoryController;

Route::apiResource('category-groups', CategoryGroupController::class);
Route::apiResource('categories', CategoryController::class);
