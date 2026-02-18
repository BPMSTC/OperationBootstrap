import React, { useEffect, useMemo, useState } from "react";

function toBool(value) {
  return value === true || value === 1 || value === "1" || value === "true";
}

async function apiFetch(url, options = {}) {
  const res = await fetch(url, {
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...(options.headers ?? {}),
    },
    ...options,
  });

  let body = null;
  const text = await res.text();
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }

  if (!res.ok) {
    const err = new Error("Request failed");
    err.status = res.status;
    err.body = body;
    throw err;
  }

  return body;
}

function errorToString(err) {
  if (!err) return "";
  if (typeof err === "string") return err;

  const body = err.body;
  if (body?.errors && typeof body.errors === "object") {
    const lines = [];
    for (const [field, msgs] of Object.entries(body.errors)) {
      if (Array.isArray(msgs)) msgs.forEach((m) => lines.push(`${field}: ${m}`));
      else lines.push(`${field}: ${String(msgs)}`);
    }
    return lines.join("\n");
  }

  if (body?.message) return body.message;
  return err.message || "Unknown error";
}

export default function Index() {
  // List state
  const [rows, setRows] = useState([]);
  const [meta, setMeta] = useState(null);
  const [links, setLinks] = useState(null);

  // Supporting data
  const [groupOptions, setGroupOptions] = useState([]); // category groups

  // Global UI state
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  // Create form state
  const [createName, setCreateName] = useState("");
  const [createGroupId, setCreateGroupId] = useState("");
  const [createParentId, setCreateParentId] = useState("");
  const [createSortOrder, setCreateSortOrder] = useState("");
  const [createIsActive, setCreateIsActive] = useState(true);

  // Inline edit state
  const [editingId, setEditingId] = useState(null);
  const [editName, setEditName] = useState("");
  const [editGroupId, setEditGroupId] = useState("");
  const [editParentId, setEditParentId] = useState("");
  const [editSortOrder, setEditSortOrder] = useState("");
  const [editIsActive, setEditIsActive] = useState(true);

  const parentOptions = useMemo(() => {
    // Build parent list from current rows
    // We'll filter out the current editing row later in the select rendering
    return rows.map((r) => ({
      id: r.id,
      label: `${r.name} (#${r.id})`,
    }));
  }, [rows]);

  const canSubmitCreate = useMemo(() => {
    return createName.trim().length > 0 && String(createGroupId).trim().length > 0;
  }, [createName, createGroupId]);

  async function load(url = "/api/categories") {
    try {
      setLoading(true);
      setError("");

      const json = await apiFetch(url, { method: "GET" });
      setRows(Array.isArray(json?.data) ? json.data : []);
      setMeta(json?.meta ?? null);
      setLinks(json?.links ?? null);
    } catch (e) {
      setError(errorToString(e));
    } finally {
      setLoading(false);
    }
  }

  async function loadCategoryGroupsForDropdown() {
    // Fetch all groups (handle pagination just in case you ever add more than 25)
    try {
      setError("");
      let all = [];
      let nextUrl = "/api/category-groups";
      for (let guard = 0; guard < 20 && nextUrl; guard++) {
        const json = await apiFetch(nextUrl, { method: "GET" });
        if (Array.isArray(json?.data)) all = all.concat(json.data);
        nextUrl = json?.links?.next ?? null;
      }
      setGroupOptions(all);
    } catch (e) {
      // Not fatal for page load, but you won't be able to create/edit properly
      setError((prev) => [prev, "Failed to load category groups.", errorToString(e)].filter(Boolean).join("\n"));
    }
  }

  useEffect(() => {
    // Load dropdown data and first page of categories
    loadCategoryGroupsForDropdown();
    load();
  }, []);

  function startEdit(row) {
    setEditingId(row.id);
    setEditName(row.name ?? "");
    setEditGroupId(row.category_group_id ?? "");
    setEditParentId(row.parent_id ?? "");
    setEditSortOrder(row.sort_order ?? "");
    setEditIsActive(toBool(row.is_active));
    setError("");
  }

  function cancelEdit() {
    setEditingId(null);
    setEditName("");
    setEditGroupId("");
    setEditParentId("");
    setEditSortOrder("");
    setEditIsActive(true);
  }

  async function createRow(e) {
    e.preventDefault();
    if (!canSubmitCreate || busy) return;

    try {
      setBusy(true);
      setError("");

      await apiFetch("/api/categories", {
        method: "POST",
        body: JSON.stringify({
          name: createName.trim(),
          category_group_id: Number(createGroupId),
          parent_id: createParentId === "" ? null : Number(createParentId),
          sort_order: createSortOrder === "" ? null : Number(createSortOrder),
          is_active: !!createIsActive,
        }),
      });

      // reset form
      setCreateName("");
      setCreateGroupId("");
      setCreateParentId("");
      setCreateSortOrder("");
      setCreateIsActive(true);

      await load(meta?.path ? `${meta.path}?page=${meta.current_page}` : "/api/categories");
    } catch (e2) {
      setError(errorToString(e2));
    } finally {
      setBusy(false);
    }
  }

  async function saveEdit(rowId) {
    if (busy) return;

    try {
      setBusy(true);
      setError("");

      await apiFetch(`/api/categories/${rowId}`, {
        method: "PATCH",
        body: JSON.stringify({
          name: editName.trim(),
          category_group_id: editGroupId === "" ? null : Number(editGroupId),
          parent_id: editParentId === "" ? null : Number(editParentId),
          sort_order: editSortOrder === "" ? null : Number(editSortOrder),
          is_active: !!editIsActive,
        }),
      });

      cancelEdit();
      await load(meta?.path ? `${meta.path}?page=${meta.current_page}` : "/api/categories");
    } catch (e) {
      setError(errorToString(e));
    } finally {
      setBusy(false);
    }
  }

  async function deleteRow(rowId) {
    if (busy) return;

    const ok = window.confirm("Delete this category? This cannot be undone.");
    if (!ok) return;

    try {
      setBusy(true);
      setError("");

      await apiFetch(`/api/categories/${rowId}`, { method: "DELETE" });

      cancelEdit();

      const currentCount = rows.length;
      const isLastItemOnPage = currentCount === 1 && meta?.current_page > 1;

      if (isLastItemOnPage && meta?.path) {
        await load(`${meta.path}?page=${meta.current_page - 1}`);
      } else {
        await load(meta?.path ? `${meta.path}?page=${meta.current_page}` : "/api/categories");
      }
    } catch (e) {
      setError(errorToString(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={{ padding: 16, maxWidth: 1200, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700 }}>Categories</h1>

      <div style={{ marginTop: 12, display: "flex", gap: 8, flexWrap: "wrap" }}>
        <button onClick={() => load()} disabled={loading || busy}>
          Refresh
        </button>

        {links?.prev && (
          <button onClick={() => load(links.prev)} disabled={loading || busy}>
            Prev
          </button>
        )}
        {links?.next && (
          <button onClick={() => load(links.next)} disabled={loading || busy}>
            Next
          </button>
        )}

        {meta && (
          <span style={{ alignSelf: "center" }}>
            Page {meta.current_page} of {meta.last_page} — Total {meta.total}
          </span>
        )}
      </div>

      {/* Create */}
      <form
        onSubmit={createRow}
        style={{
          marginTop: 16,
          padding: 12,
          border: "1px solid #e5e5e5",
          borderRadius: 8,
        }}
      >
        <h2 style={{ fontSize: 16, fontWeight: 600, marginBottom: 8 }}>Create Category</h2>

        <div style={{ display: "grid", gridTemplateColumns: "2fr 1.5fr 1.5fr 1fr 1fr auto", gap: 8 }}>
          <input
            value={createName}
            onChange={(e) => setCreateName(e.target.value)}
            placeholder="Name (required)"
            disabled={busy}
          />

          <select
            value={createGroupId}
            onChange={(e) => setCreateGroupId(e.target.value)}
            disabled={busy}
          >
            <option value="">Select group (required)</option>
            {groupOptions.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name} (#{g.id})
              </option>
            ))}
          </select>

          <select
            value={createParentId}
            onChange={(e) => setCreateParentId(e.target.value)}
            disabled={busy}
          >
            <option value="">Parent: None</option>
            {parentOptions.map((p) => (
              <option key={p.id} value={p.id}>
                {p.label}
              </option>
            ))}
          </select>

          <input
            value={createSortOrder}
            onChange={(e) => setCreateSortOrder(e.target.value)}
            placeholder="Sort order"
            type="number"
            disabled={busy}
          />

          <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <input
              type="checkbox"
              checked={createIsActive}
              onChange={(e) => setCreateIsActive(e.target.checked)}
              disabled={busy}
            />
            Active
          </label>

          <button type="submit" disabled={!canSubmitCreate || busy}>
            {busy ? "Working..." : "Create"}
          </button>
        </div>
      </form>

      {/* Errors */}
      {error && (
        <pre
          style={{
            marginTop: 12,
            padding: 12,
            background: "#fff5f5",
            border: "1px solid #ffd6d6",
            borderRadius: 8,
            color: "#b00020",
            whiteSpace: "pre-wrap",
          }}
        >
          {error}
        </pre>
      )}

      {/* List */}
      <div style={{ marginTop: 16, overflowX: "auto" }}>
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr>
              {["ID", "Name", "Group", "Parent", "Sort", "Active", "Actions"].map((h) => (
                <th
                  key={h}
                  style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>

          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} style={{ padding: 8 }}>
                  Loading...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={7} style={{ padding: 8 }}>
                  (No categories)
                </td>
              </tr>
            ) : (
              rows.map((row) => {
                const isEditing = editingId === row.id;

                return (
                  <tr key={row.id}>
                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>{row.id}</td>

                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                      {isEditing ? (
                        <input
                          value={editName}
                          onChange={(e) => setEditName(e.target.value)}
                          disabled={busy}
                        />
                      ) : (
                        row.name
                      )}
                    </td>

                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                      {isEditing ? (
                        <select
                          value={editGroupId ?? ""}
                          onChange={(e) => setEditGroupId(e.target.value)}
                          disabled={busy}
                        >
                          <option value="">Select group</option>
                          {groupOptions.map((g) => (
                            <option key={g.id} value={g.id}>
                              {g.name} (#{g.id})
                            </option>
                          ))}
                        </select>
                      ) : (
                        row.category_group_id
                      )}
                    </td>

                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                      {isEditing ? (
                        <select
                          value={editParentId ?? ""}
                          onChange={(e) => setEditParentId(e.target.value)}
                          disabled={busy}
                        >
                          <option value="">Parent: None</option>
                          {parentOptions
                            .filter((p) => p.id !== row.id) // prevent self-parent
                            .map((p) => (
                              <option key={p.id} value={p.id}>
                                {p.label}
                              </option>
                            ))}
                        </select>
                      ) : (
                        row.parent_id ?? "—"
                      )}
                    </td>

                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                      {isEditing ? (
                        <input
                          value={editSortOrder ?? ""}
                          onChange={(e) => setEditSortOrder(e.target.value)}
                          type="number"
                          disabled={busy}
                        />
                      ) : (
                        row.sort_order ?? "—"
                      )}
                    </td>

                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                      {isEditing ? (
                        <input
                          type="checkbox"
                          checked={!!editIsActive}
                          onChange={(e) => setEditIsActive(e.target.checked)}
                          disabled={busy}
                        />
                      ) : (
                        toBool(row.is_active) ? "Yes" : "No"
                      )}
                    </td>

                    <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                      {!isEditing ? (
                        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                          <button onClick={() => startEdit(row)} disabled={busy}>
                            Edit
                          </button>
                          <button onClick={() => deleteRow(row.id)} disabled={busy}>
                            Delete
                          </button>
                        </div>
                      ) : (
                        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                          <button
                            onClick={() => saveEdit(row.id)}
                            disabled={busy || editName.trim().length === 0 || String(editGroupId).trim().length === 0}
                          >
                            Save
                          </button>
                          <button onClick={cancelEdit} disabled={busy}>
                            Cancel
                          </button>
                          <button onClick={() => deleteRow(row.id)} disabled={busy}>
                            Delete
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <p style={{ marginTop: 12, opacity: 0.7 }}>
        Uses your API: <code>/api/categories</code> and loads groups from <code>/api/category-groups</code>.
      </p>
    </div>
  );
}
