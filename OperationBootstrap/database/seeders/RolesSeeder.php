<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;

class RolesSeeder extends Seeder
{
    public function run(): void
    {
        $now = now();

        // idempotent using unique key: roles.name
        $roles = [
            ['name' => 'Admin',     'description' => 'Full system access'],
            ['name' => 'Staff',     'description' => 'Staff access'],
            ['name' => 'Volunteer', 'description' => 'Volunteer access'],
            ['name' => 'Client',    'description' => 'Client access'],
        ];

        foreach ($roles as $role) {
            DB::table('roles')->updateOrInsert(
                ['name' => $role['name']],
                [
                    'description' => $role['description'],
                    'created_at' => $now,
                    'updated_at' => $now,
                    // created_by_user_id / updated_by_user_id can be null for baseline
                ]
            );
        }
    }
}
