<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;

class InventoryItemsSeeder extends Seeder
{
    public function run(): void
    {
        $now = now();

        $pbCategoryId = DB::table('categories')->where('name', 'Peanut Butter')->value('id');
        $tunaCategoryId = DB::table('categories')->where('name', 'Canned Tuna')->value('id');

        $items = [
            ['name' => 'Peanut Butter - 16oz', 'category_id' => $pbCategoryId, 'is_baseline' => true, 'is_available' => true, 'is_active' => true],
            ['name' => 'Tuna - 4 pack',        'category_id' => $tunaCategoryId, 'is_baseline' => false,'is_available' => true, 'is_active' => true],
        ];

        foreach ($items as $i) {
            // No unique constraint in your SQL, so choose a stable key.
            // Using (name + category_id) as a practical unique key.
            DB::table('inventory_items')->updateOrInsert(
                ['name' => $i['name'], 'category_id' => $i['category_id']],
                [
                    'is_baseline' => $i['is_baseline'],
                    'is_available' => $i['is_available'],
                    'is_active' => $i['is_active'],
                    'created_at' => $now,
                    'updated_at' => $now,
                ]
            );
        }
    }
}
