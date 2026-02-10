// database/migrations/2026_02_09_000007_create_categories_table.php
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('categories', function (Blueprint $table) {
            $table->id(); // BIGINT UNSIGNED AUTO_INCREMENT

            $table->unsignedBigInteger('category_group_id');
            $table->string('name', 150);
            $table->unsignedBigInteger('parent_id')->nullable();
            $table->integer('sort_order')->nullable();
            $table->boolean('is_active')->default(true);

            // Audit
            $table->unsignedBigInteger('created_by_user_id')->nullable();
            $table->unsignedBigInteger('updated_by_user_id')->nullable();

            // Laravel standard timestamps + soft deletes
            $table->timestamps();
            $table->softDeletes();

            // Unique + indexes from your SQL
            $table->unique(['category_group_id', 'name'], 'categories_group_name_unique');
            $table->index('category_group_id', 'idx_categories_category_group_id');
            $table->index('parent_id', 'idx_categories_parent_id');
        });

        // Add FKs after table exists (matches your FK names + delete behaviors)
        Schema::table('categories', function (Blueprint $table) {
            $table->foreign('category_group_id', 'categories_category_group_fk')
                ->references('id')->on('category_groups')
                ->restrictOnDelete();

            $table->foreign('parent_id', 'categories_parent_fk')
                ->references('id')->on('categories')
                ->nullOnDelete();

            $table->foreign('created_by_user_id', 'categories_created_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();

            $table->foreign('updated_by_user_id', 'categories_updated_by_fk')
                ->references('id')->on('users')
                ->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('categories', function (Blueprint $table) {
            $table->dropForeign('categories_category_group_fk');
            $table->dropForeign('categories_parent_fk');
            $table->dropForeign('categories_created_by_fk');
            $table->dropForeign('categories_updated_by_fk');
        });

        Schema::dropIfExists('categories');
    }
};
