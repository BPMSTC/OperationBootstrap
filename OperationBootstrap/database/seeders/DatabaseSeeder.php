<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;

class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        // Baseline (safe, idempotent)
        $this->call([
            RolesSeeder::class,
            AdminUserSeeder::class,
            CategoryGroupsSeeder::class,
            CategoriesSeeder::class,
            InventoryItemsSeeder::class,
            ReferringOrganizationsSeeder::class,
        ]);

        // Demo-only data (optional)
        // if (app()->environment(['local', 'testing'])) {
        //     $this->call(DemoDataSeeder::class);
        // }
    }
}
