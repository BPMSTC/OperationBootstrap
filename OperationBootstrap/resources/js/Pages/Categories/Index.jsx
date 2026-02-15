import React, { useEffect, useState } from "react";

export default function Index() {
  const [rows, setRows] = useState([]);
  const [meta, setMeta] = useState(null);
  const [links, setLinks] = useState(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  async function load(url = "/api/categories") {
    try {
      setLoading(true);
      setError("");

      const res = await fetch(url, {
        headers: { Accept: "application/json" },
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(`GET ${url} failed (${res.status}): ${text}`);
      }

      const json = await res.json();

      setRows(Array.isArray(json?.data) ? json.data : []);
      setMeta(json?.meta ?? null);
      setLinks(json?.links ?? null);
    } catch (e) {
      setError(e?.message ?? "Unknown error");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <div style={{ padding: 16 }}>
      <h1>Categories</h1>

      <div style={{ marginTop: 12, display: "flex", gap: 8 }}>
        <button onClick={() => load()} disabled={loading}>
          Refresh
        </button>

        {links?.prev && (
          <button onClick={() => load(links.prev)} disabled={loading}>
            Prev
          </button>
        )}
        {links?.next && (
          <button onClick={() => load(links.next)} disabled={loading}>
            Next
          </button>
        )}
      </div>

      {meta && (
        <p style={{ marginTop: 10 }}>
          Page {meta.current_page} of {meta.last_page} — Total: {meta.total}
        </p>
      )}

      {loading && <p>Loading…</p>}
      {error && <pre style={{ color: "crimson" }}>{error}</pre>}

      {!loading && !error && (
        <table
          style={{ marginTop: 12, borderCollapse: "collapse", width: "100%" }}
        >
          <thead>
            <tr>
              {["ID", "Name", "Group", "Parent", "Sort", "Active"].map((h) => (
                <th
                  key={h}
                  style={{
                    textAlign: "left",
                    borderBottom: "1px solid #ddd",
                    padding: "8px",
                  }}
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ padding: 8 }}>
                  (No categories)
                </td>
              </tr>
            ) : (
              rows.map((c) => (
                <tr key={c.id}>
                  <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                    {c.id}
                  </td>
                  <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                    {c.name}
                  </td>
                  <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                    {c.category_group_id}
                  </td>
                  <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                    {c.parent_id ?? "—"}
                  </td>
                  <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                    {c.sort_order ?? "—"}
                  </td>
                  <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>
                    {c.is_active ? "Yes" : "No"}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}
