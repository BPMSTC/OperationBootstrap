<?php

namespace App\Http\Controllers\Pages;

use App\Http\Controllers\Controller;
use Inertia\Inertia;

class CategoryGroupPageController extends Controller
{
    public function index()
    {
        return Inertia::render('CategoryGroups/Index');
    }
}
