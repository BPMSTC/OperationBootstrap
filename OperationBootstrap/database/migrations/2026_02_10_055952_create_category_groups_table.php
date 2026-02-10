<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('category_groups', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->string('name', 100)->unique('category_groups_name_unique');
            $table->integer('sort_order')->nullable();
            $table->boolean('is_active')->default(true);

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel standard timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Index from your SQL
            $table->index('is_active', 'idx_category_groups_is_active');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('category_groups', function (Blueprint $table) {
            $table->foreign('created_by_user_id', 'category_groups_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'category_groups_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('category_groups', function (Blueprint $table) {
            $table->dropForeign('category_groups_created_by_fk');
            $table->dropForeign('category_groups_updated_by_fk');
        });

        Schema::dropIfExists('category_groups');
    }
};
