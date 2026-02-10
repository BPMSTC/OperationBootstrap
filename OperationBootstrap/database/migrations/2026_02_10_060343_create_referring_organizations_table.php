<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('referring_organizations', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->string('name', 255);
            $table->string('type', 100)->nullable();
            $table->string('phone_number', 50)->nullable();
            $table->string('email', 255)->nullable();

            $table->string('address_line1', 255)->nullable();
            $table->string('address_line2', 255)->nullable();
            $table->string('city', 120)->nullable();
            $table->string('state', 60)->nullable();
            $table->string('postal_code', 20)->nullable();

            $table->string('primary_contact_name', 255)->nullable();
            $table->text('notes')->nullable();

            $table->boolean('is_active')->default(true);

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel standard timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Indexes from your SQL
            $table->index('is_active', 'idx_referring_orgs_is_active');
            $table->index('name', 'idx_referring_orgs_name');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('referring_organizations', function (Blueprint $table) {
            $table->foreign('created_by_user_id', 'referring_orgs_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'referring_orgs_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('referring_organizations', function (Blueprint $table) {
            $table->dropForeign('referring_orgs_created_by_fk');
            $table->dropForeign('referring_orgs_updated_by_fk');
        });

        Schema::dropIfExists('referring_organizations');
    }
};
