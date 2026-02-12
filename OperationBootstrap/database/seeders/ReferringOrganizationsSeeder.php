<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\DB;

class ReferringOrganizationsSeeder extends Seeder
{
    public function run(): void
    {
        $now = now();

        $orgs = [
            [
                'name' => 'Portage County Outreach',
                'type' => 'Nonprofit',
                'phone_number' => '555-0101',
                'email' => 'referrals@pco.example',
                'address_line1' => '123 Main St',
                'address_line2' => null,
                'city' => 'Stevens Point',
                'state' => 'WI',
                'postal_code' => '54481',
                'primary_contact_name' => 'Case Worker A',
                'notes' => 'Demo org',
                'is_active' => true,
            ],
            [
                'name' => 'Community Health Partners',
                'type' => 'Clinic',
                'phone_number' => '555-0202',
                'email' => 'intake@chp.example',
                'address_line1' => '456 Oak Ave',
                'address_line2' => 'Suite 200',
                'city' => 'Stevens Point',
                'state' => 'WI',
                'postal_code' => '54481',
                'primary_contact_name' => 'Care Coordinator',
                'notes' => null,
                'is_active' => true,
            ],
        ];

        foreach ($orgs as $o) {
            // No unique constraint in your SQL, so use name as a practical stable key
            DB::table('referring_organizations')->updateOrInsert(
                ['name' => $o['name']],
                [
                    'type' => $o['type'],
                    'phone_number' => $o['phone_number'],
                    'email' => $o['email'],
                    'address_line1' => $o['address_line1'],
                    'address_line2' => $o['address_line2'],
                    'city' => $o['city'],
                    'state' => $o['state'],
                    'postal_code' => $o['postal_code'],
                    'primary_contact_name' => $o['primary_contact_name'],
                    'notes' => $o['notes'],
                    'is_active' => $o['is_active'],
                    'created_at' => $now,
                    'updated_at' => $now,
                ]
            );
        }
    }
}
