<?php

namespace App\Http\Controllers\Pages;

use App\Http\Controllers\Controller;
use Inertia\Inertia;

class CategoryPageController extends Controller
{
    public function index()
    {
        return Inertia::render('Categories/Index');
    }
}
