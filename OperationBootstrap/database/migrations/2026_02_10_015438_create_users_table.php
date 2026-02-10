<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('users', function (Blueprint $table) {
            $table->id();

            // Auth
            $table->string('email', 255)->nullable()->unique('users_email_unique');
            $table->string('password', 255)->nullable();
            $table->rememberToken();
            $table->timestamp('email_verified_at')->nullable();
            $table->timestamp('last_login_at')->nullable();

            // Profile
            $table->string('phone_number', 50)->nullable();
            $table->string('first_name', 255)->nullable();
            $table->string('last_name', 255)->nullable();
            $table->string('name', 255)->nullable();
            $table->string('address_line1', 255)->nullable();
            $table->string('address_line2', 255)->nullable();
            $table->string('city', 120)->nullable();
            $table->string('state', 60)->nullable();
            $table->string('postal_code', 20)->nullable();
            $table->date('date_of_birth')->nullable();

            $table->string('default_preference', 20)->default('ask');
            $table->boolean('is_active')->default(true);

            // Audit (self-referencing users)
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel standard timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Indexes you called out
            $table->index('is_active', 'idx_users_is_active');
            $table->index('last_login_at', 'idx_users_last_login_at');
            $table->index('deleted_at', 'idx_users_deleted_at');
        });

        // Add self-referencing FKs after table exists
        Schema::table('users', function (Blueprint $table) {
            $table->foreign('created_by_user_id', 'users_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'users_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('users', function (Blueprint $table) {
            $table->dropForeign('users_created_by_fk');
            $table->dropForeign('users_updated_by_fk');
        });

        Schema::dropIfExists('users');
    }
};
