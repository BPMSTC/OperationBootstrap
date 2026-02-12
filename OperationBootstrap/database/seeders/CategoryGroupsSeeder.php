<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;

class CategoryGroupsSeeder extends Seeder
{
    public function run(): void
    {
        $now = now();

        $groups = [
            ['name' => 'Food', 'sort_order' => 1, 'is_active' => true],
            ['name' => 'Non-Food', 'sort_order' => 2, 'is_active' => true],
        ];

        foreach ($groups as $g) {
            DB::table('category_groups')->updateOrInsert(
                ['name' => $g['name']],
                [
                    'sort_order' => $g['sort_order'],
                    'is_active' => $g['is_active'],
                    'created_at' => $now,
                    'updated_at' => $now,
                ]
            );
        }
    }
}
