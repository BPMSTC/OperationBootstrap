<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;

class AdminUserSeeder extends Seeder
{
    public function run(): void
    {
        $now = now();

        // Pull these from env so each developer can set their own local admin
        $email = env('SEED_ADMIN_EMAIL', 'admin@example.com');
        $password = env('SEED_ADMIN_PASSWORD', 'ChangeMe123!');

        // Create/update user by unique email
        DB::table('users')->updateOrInsert(
            ['email' => $email],
            [
                'name' => 'System Admin',
                'first_name' => 'System',
                'last_name' => 'Admin',
                'password' => Hash::make($password),
                'is_active' => true,
                'default_preference' => 'ask',
                'created_at' => $now,
                'updated_at' => $now,
            ]
        );

        $userId = DB::table('users')->where('email', $email)->value('id');
        $adminRoleId = DB::table('roles')->where('name', 'Admin')->value('id');

        // Attach role (idempotent): role_user has composite PK
        DB::table('role_user')->updateOrInsert(
            ['user_id' => $userId, 'role_id' => $adminRoleId],
            ['created_at' => $now, 'updated_at' => $now]
        );
    }
}
