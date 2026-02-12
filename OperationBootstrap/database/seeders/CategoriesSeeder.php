<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;

class CategoriesSeeder extends Seeder
{
    public function run(): void
    {
        $now = now();

        $foodGroupId = DB::table('category_groups')->where('name', 'Food')->value('id');
        $nonFoodGroupId = DB::table('category_groups')->where('name', 'Non-Food')->value('id');

        // Parents
        $parents = [
            ['group_id' => $foodGroupId, 'name' => 'Proteins', 'sort_order' => 1],
            ['group_id' => $foodGroupId, 'name' => 'Produce',  'sort_order' => 2],
            ['group_id' => $nonFoodGroupId, 'name' => 'Hygiene', 'sort_order' => 1],
        ];

        foreach ($parents as $p) {
            DB::table('categories')->updateOrInsert(
                ['category_group_id' => $p['group_id'], 'name' => $p['name']],
                [
                    'parent_id' => null,
                    'sort_order' => $p['sort_order'],
                    'is_active' => true,
                    'created_at' => $now,
                    'updated_at' => $now,
                ]
            );
        }

        // Children (example)
        $proteinsId = DB::table('categories')
            ->where('category_group_id', $foodGroupId)
            ->where('name', 'Proteins')
            ->value('id');

        $children = [
            ['group_id' => $foodGroupId, 'name' => 'Canned Tuna', 'parent_id' => $proteinsId, 'sort_order' => 1],
            ['group_id' => $foodGroupId, 'name' => 'Peanut Butter', 'parent_id' => $proteinsId, 'sort_order' => 2],
        ];

        foreach ($children as $c) {
            DB::table('categories')->updateOrInsert(
                ['category_group_id' => $c['group_id'], 'name' => $c['name']],
                [
                    'parent_id' => $c['parent_id'],
                    'sort_order' => $c['sort_order'],
                    'is_active' => true,
                    'created_at' => $now,
                    'updated_at' => $now,
                ]
            );
        }
    }
}
