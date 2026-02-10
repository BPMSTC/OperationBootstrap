<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('household_members', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->unsignedBigInteger('client_user_id');
            $table->string('full_name', 255);
            $table->date('date_of_birth')->nullable();
            $table->unsignedTinyInteger('age_years')->nullable();
            $table->date('age_as_of_date')->nullable();

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Standardized Laravel timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Indexes from your SQL
            $table->index('client_user_id', 'idx_household_members_client_user_id');
            $table->index('date_of_birth', 'idx_household_members_date_of_birth');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('household_members', function (Blueprint $table) {
            $table->foreign('client_user_id', 'household_members_client_fk')
                ->references('id')->on('users')
                ->cascadeOnDelete();

            $table->foreign('created_by_user_id', 'household_members_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'household_members_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('household_members', function (Blueprint $table) {
            $table->dropForeign('household_members_client_fk');
            $table->dropForeign('household_members_created_by_fk');
            $table->dropForeign('household_members_updated_by_fk');
        });

        Schema::dropIfExists('household_members');
    }
};
