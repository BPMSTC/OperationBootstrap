<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('client_profiles', function (Blueprint $table) {
            // PK + FK to users(id)
            $table->unsignedBigInteger('user_id');

            $table->string('employment_status', 100)->nullable();
            $table->decimal('earned_income_monthly', 10, 2)->nullable();
            $table->boolean('is_unhoused')->default(false);

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Standardized Laravel timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            $table->primary('user_id');

            // NOTE: your SQL does not define indexes here beyond the PK
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('client_profiles', function (Blueprint $table) {
            $table->foreign('user_id', 'client_profiles_user_fk')
                ->references('id')->on('users')
                ->cascadeOnDelete();

            $table->foreign('created_by_user_id', 'client_profiles_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'client_profiles_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('client_profiles', function (Blueprint $table) {
            $table->dropForeign('client_profiles_user_fk');
            $table->dropForeign('client_profiles_created_by_fk');
            $table->dropForeign('client_profiles_updated_by_fk');
        });

        Schema::dropIfExists('client_profiles');
    }
};
